using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using Avro;
using Avro.Generic;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry.Serdes;
using Confluent.SchemaRegistry;
using Confluent.Kafka;
using MealTimeMS.ExclusionProfiles;
using MealTimeMS.Data;
using com.bruker.paser.avro;
using MealTimeMS.ExclusionProfiles.MachineLearningGuided;
using System.IO;
using MealTimeMS.IO;
using MealTimeMS.Util;
using System.Diagnostics;
namespace MealTimeMS.RunTime
{
    class BrukerInstrumentConnection
    {
        static List<IDs> psmTracker;
        const double timeOutThresholdMiliseconds = 10000; //After receiving the acquisition-stopped signal from the paser_control kafka topic, will wait for this amount of miliseconds to elapse after the last message is consumed by psm or ms2 thread before stopping the psm/ms2 consuming thread
        static int psmReceived = 1; //will be updated to 1 everytime the prolucid processing thread receives a psm. Used for the timer in the paserControl thread 

        public static void ConnectRealTime(ExclusionProfile exclusionProfile)
        {
            Connect(exclusionProfile, "", "", BrukerConnectionEnum.ProLucidConnectionOnly, false, false, _group_id: "MealTime-MS-Consumer");
        }

        public static void Connect(ExclusionProfile exclusionProfile, String brukerDotDFolder, String sqtFile,
            BrukerConnectionEnum connectionMode, bool runWithoutExclusionProfile = false, bool startAcquisitionSimulator = true, string _group_id = "MealTime-MS-Consumer-Group")
        {
            string bootstrapServers = GlobalVar.kafka_url;
            string schemaRegistryUrl = GlobalVar.schemaRegistry_url;
            string topicName = "psm_prolucid";

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = bootstrapServers
            };

            var schemaRegistryConfig = new SchemaRegistryConfig
            {
                Url = schemaRegistryUrl
            };

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = _group_id,
                AutoOffsetReset = AutoOffsetReset.Latest,

            };

            var avroSerializerConfig = new AvroSerializerConfig
            {
                // optional Avro serializer properties:
                BufferBytes = 100
            };

            int counter_psm = 0;
            int counter_ms2 = 0;
            CancellationTokenSource cts = new CancellationTokenSource();
            Task BrukerInputProcessorThread;
            if (runWithoutExclusionProfile == false && connectionMode == BrukerConnectionEnum.ProLucidConnectionOnly)
            {
                BrukerInputProcessorThread = Task.Run(() => BrukerInputScheduler.StartProcessing(exclusionProfile, cts.Token));
            }
            var consumeTask_paserControl = Task.Run(() =>
            {
                using (var schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig))
                using (var consumer =
                    new ConsumerBuilder<string, ProducerState>(consumerConfig)
                       .SetValueDeserializer(new AvroDeserializer<ProducerState>(schemaRegistry, null).AsSyncOverAsync())
                        .SetErrorHandler((_, e) => Console.WriteLine($"Error: {e.Reason}"))
                        .Build())
                {
                    consumer.Subscribe("paser_control");
                    ResetConsumerOffset(consumer);
                    Console.WriteLine("Consumer connecting to kafka broker, topic: paser_control");
                    try
                    {
                        while (true)
                        {
                            try
                            {
                                var consumeResult = consumer.Consume(cts.Token);
                               
                                var paserControlMessage = consumeResult.Message.Value;
                                if (paserControlMessage.control_type == 0)
                                {
                                    Console.WriteLine("End-of-acquisition message received from Paser-control. Stopping Mealtime-MS if no psm is received in 30 seconds");
                                    Interlocked.Exchange(ref psmReceived, 0);
                                    Stopwatch sw = new Stopwatch();
                                    sw.Start();
                                    while (true)
                                    {
                                        if (sw.ElapsedMilliseconds > timeOutThresholdMiliseconds)
                                        {
                                            //The psm thread sets psmReceived to 1 whenever a psm is received
                                            if (psmReceived == 0)
                                            {
                                                //if after 30s psmReceived is still 0, it means no psm has been received and we can activate the cancellation token to the psm thread
                                                break;
                                            }
                                            else
                                            {
                                                psmReceived = 0;
                                                sw.Restart();
                                            }
                                        }
                                    }

                                    cts.Cancel();
                                }
                            }
                            catch (ConsumeException e)
                            {
                                Console.WriteLine($"Consume error: {e.Error.Reason}");
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine("Operation cancelled - kafka - paser-control");
                        consumer.Close();
                    }
                }
            });
            if (connectionMode == BrukerConnectionEnum.ProLucidConnectionOnly)
            {
                var consumeTask_Prolucid = Task.Run(() =>
            {
                using (var schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig))
                using (var consumer =
                    new ConsumerBuilder<string, PsmProlucid>(consumerConfig)
                       .SetValueDeserializer(new AvroDeserializer<PsmProlucid>(schemaRegistry, null).AsSyncOverAsync())
                        .SetErrorHandler((_, e) => Console.WriteLine($"Error: {e.Reason}"))
                        .Build())
                {
                    consumer.Subscribe(topicName);
                    ResetConsumerOffset(consumer);
                    Console.WriteLine("Consumer connecting to kafka broker, topic {0}", topicName);
                    Console.WriteLine("Ready to process incoming messages");
                    try
                    {

                        while (true)
                        {
                            try
                            {
                                var consumeResult = consumer.Consume(cts.Token);
                                var psm = consumeResult.Message.Value;
                                IDs id = IDs.getIDsFromPSMProlucid(psm);// maybe will need to change the rt unit to min

                                if (psm.ms2_id % GlobalVar.ScansPerOutput == 0)
                                {
                                    Console.WriteLine($"key: {consumeResult.Message.Key}, psm- ms2_id: {psm.ms2_id}, rt: {psm.rt}, mono mz: {psm.mono_mz},charge: {psm.charge}, " +
                                    $"parent_id: {psm.parent_id}");
                                    if (id == null)
                                    {
                                        Console.WriteLine("id = null");
                                    }
                                    else
                                    {
                                        Console.WriteLine("id- ms2_id: {0}, xcor: {1}, dCN: {2}, sequence: {3}", id.getScanNum(), id.getXCorr(), id.getDeltaCN(), id.getPeptideSequence());
                                    }
                                    Console.WriteLine("Offset {0}", consumeResult.Offset);
                                }
                                counter_psm++;
                                psmReceived = 1;
                                if (!runWithoutExclusionProfile)
                                {
                                    BrukerInputScheduler.EnqueueProlucidPSM(id);
                                    // exclusionProfile.evaluateIdentificationAndUpdateCurrentTime(id);
                                }
                                else
                                {
                                        if (false) // id != null)
                                    {
                                        //psmTracker.Add(id);
                                        sw.WriteLine(String.Join(separator: "\t",
                                          id.getScanNum(), id.getPeptideSequence(), id.getPeptideSequence_withModification(), id.getScanTime(),
                                          id.getPeptideMass(), id.getXCorr(), id.getDeltaCN(),
                                          id.getParentProteinAccessionsAsString()));
                                    }
                                }
                            }
                            catch (ConsumeException e)
                            {
                                Console.WriteLine($"Consume error: {e.Error.Reason}");
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine("Operation cancelled - kafka - psmStream consumption");
                        consumer.Close();
                    }
                }
            });

            }
            if (connectionMode == BrukerConnectionEnum.MS2ConnectionOnly)
            {
                string ms2TopicName = "ms2_spectra";
                var consumeTask_ms2 = Task.Run(() =>
            {
                using (var schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig))
                using (var consumer =
                    new ConsumerBuilder<string, PasefMs2Spectrum>(consumerConfig)
                       .SetValueDeserializer(new AvroDeserializer<PasefMs2Spectrum>(schemaRegistry, null).AsSyncOverAsync())
                        .SetErrorHandler((_, e) => Console.WriteLine($"Error: {e.Reason}"))
                        .Build())
                {
                    consumer.Subscribe(ms2TopicName);
                    ResetConsumerOffset(consumer);
                    Console.WriteLine("Consumer connecting to kafka broker, topic {0}", ms2TopicName);
                    Console.WriteLine("Ready to process incoming messages");
                    try
                    {
                        while (true)
                        {
                            try
                            {
                                var consumeResult = consumer.Consume(cts.Token);
                                var spectra = consumeResult.Message.Value;
                                psmReceived = 1;
                                counter_ms2++;
                                if (counter_ms2 % GlobalVar.ScansPerOutput == 0)
                                {
                                    Console.WriteLine($"key: {consumeResult.Message.Key}, ms2- ms2_id: {spectra.ms2_id}, rt_sec: {spectra.rt}, " +
                                    $"charge: {spectra.charge}, " +
                                    $"parent_id: {spectra.parent_id}");
                                }
                                if (!runWithoutExclusionProfile)
                                {
                                    exclusionProfile.evaluate(Spectra.GetSpectraFromPasefMs2Spectrum(spectra));
                                    //double precursorMass = MassConverter.convertMZ(spectra.mono_mz,spectra.charge);
                                    //bool isExcluded = exclusionProfile.process_bruker_ms2(spectra.rt, precursorMass,spectra.ms2_id);
                                    //includedScanNum[spectra.ms2_id]= !isExcluded;
                                }
                                else
                                {

                                }
                            }
                            catch (ConsumeException e)
                            {
                                Console.WriteLine($"Consume error: {e.Error.Reason}");
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine("Operation cancelled - kafka - {0} consumption", ms2TopicName);
                        consumer.Close();
                    }
                }
            });

            }
            if (startAcquisitionSimulator)
            {
                CommandLineProcessingUtil.RunBrukerAcquisitionSimulator(brukerDotDFolder, sqtFile, GlobalVar.exclusionMS_ip);
            }
            
            consumeTask_paserControl.Wait(600000000);
            //consumeTask_Prolucid.Wait(600000000);
            //consumeTask_ms2.Wait(600000000);
            cts.Cancel();
            Thread.Sleep(1500);
            Console.WriteLine("Operation finished");
            Console.WriteLine("Received total {0} psms and {1} ms2", counter_psm, counter_ms2);
            //using (var schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig))
            //using (var producer =
            //    new ProducerBuilder<string, User>(producerConfig)
            //        .SetValueSerializer(new AvroSerializer<User>(schemaRegistry, avroSerializerConfig))
            //        .Build())
            //{
            //    Console.WriteLine($"{producer.Name} producing on {topicName}. Enter user names, q to exit.");

            //    int i = 1;
            //    string text;
            //    while ((text = Console.ReadLine()) != "q")
            //    {
            //        User user = new User { name = text, favorite_color = "green", favorite_number = ++i, hourly_rate = new Avro.AvroDecimal(67.99) };
            //        producer
            //            .ProduceAsync(topicName, new Message<string, User> { Key = text, Value = user })
            //            .ContinueWith(task =>
            //            {
            //                if (!task.IsFaulted)
            //                {
            //                    Console.WriteLine($"produced to: {task.Result.TopicPartitionOffset}");
            //                    return;
            //                }

            //                // Task.Exception is of type AggregateException. Use the InnerException property
            //                // to get the underlying ProduceException. In some cases (notably Schema Registry
            //                // connectivity issues), the InnerException of the ProduceException will contain
            //                // additional information pertaining to the root cause of the problem. Note: this
            //                // information is automatically included in the output of the ToString() method of
            //                // the ProduceException which is called implicitly in the below.
            //                Console.WriteLine($"error producing message: {task.Exception.InnerException}");
            //            });
            //    }
            //}


        }

        private static void ResetConsumerOffset(IConsumer<string, PsmProlucid> consumer)
        {
            Thread.Sleep(2000);
            var tp = consumer.Assignment;
            tp = consumer.Assignment;
            var tpOffset = tp.Select(c => new TopicPartitionOffset(c, new Offset(
                consumer.QueryWatermarkOffsets(c, TimeSpan.FromSeconds(10)).High.Value + 1))).ToList();
            consumer.Assign(tpOffset);
            Thread.Sleep(2000);
        }
        private static void ResetConsumerOffset(IConsumer<string, ProducerState> consumer)
        {
            Thread.Sleep(2000);
            var tp = consumer.Assignment;
            tp = consumer.Assignment;
            var tpOffset = tp.Select(c => new TopicPartitionOffset(c, new Offset(
                consumer.QueryWatermarkOffsets(c, TimeSpan.FromSeconds(10)).High.Value + 1))).ToList();
            consumer.Assign(tpOffset);
            Thread.Sleep(2000);
        }
        private static void ResetConsumerOffset(IConsumer<string, PasefMs2Spectrum> consumer)
        {
            Thread.Sleep(2000);
            var tp = consumer.Assignment;
            tp = consumer.Assignment;
            var tpOffset = tp.Select(c => new TopicPartitionOffset(c, new Offset(
                consumer.QueryWatermarkOffsets(c, TimeSpan.FromSeconds(10)).High.Value + 1))).ToList();
            consumer.Assign(tpOffset);
            Thread.Sleep(2000);
        }


        //a unit test function that connects to Kafka broker and prints all psms to a file
        //NoExclusion.RecordSpecInfo() does something similar to this function 
        static StreamWriter sw;
        public static void PrintAllProlucidPSM(String brukerDotDFolder, String sqtFile)
        {
            psmTracker = new List<IDs>();
            sw = new StreamWriter(
                Path.Combine(InputFileOrganizer.OutputFolderOfTheRun, "ProlucidPSMs.tsv"));
            sw.WriteLine(String.Join(separator: "\t", "ms2_id", "peptide_stripped", "peptide_modified", "rt(sec)", "calc_mass", "xcorr", "dCN", "accessions"));
            Connect(null, brukerDotDFolder, sqtFile, BrukerConnectionEnum.ProLucidConnectionOnly, true, _group_id: "MealTime-MS-Consumer");
            sw.Close();
        }
        public static void TestConnection()
        {
            //String libraryPath = Path.Combine(InputFileOrganizer.AssemblyDirectory, "librdkafka\\x64\\librdkafka.dll");
            //String libraryPath = Path.Combine(InputFileOrganizer.AssemblyDirectory, "EmbeddedDataFiles/librdkafka.dll");
            //Console.WriteLine(libraryPath);
            //Console.WriteLine("Type yes");
            //while (!Console.ReadLine().Equals("yes"))
            //{
            //    Thread.Sleep(1000);
            //}
            //Library.Load(libraryPath);
            Console.WriteLine("Testing connection to kafka broker");
            //GlobalVar.kafka_url = "127.0.0.1:9092";
            //GlobalVar.schemaRegistry_url = "127.0.0.1:8083";
            Connect(null, "", "", BrukerConnectionEnum.ProLucidConnectionOnly, runWithoutExclusionProfile: true, false);
        }

        public enum BrukerConnectionEnum
        {
            [Description("MS2ConnectionOnly")]
            MS2ConnectionOnly,
            [Description("ProLucidConnectionOnly")]
            ProLucidConnectionOnly

        };

    }


}
