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
        const  double timeOutThresholdMiliseconds = 300000;
        static ConcurrentDictionary<int , bool> includedScanNum;
        static int psmReceived=1; //will be updated to 1 everytime the prolucid processing thread receives a psm. Used for the timer in the paserControl thread 
        public static void DoJob()
        {


        }
        public static void Connect(ExclusionProfile exclusionProfile, BrukerConnectionEnum connectionMode, bool runWithoutExclusionProfile = false)
        {
            //string bootstrapServers = args[0];
            //string schemaRegistryUrl = args[1];
            //string topicName = args[2];
            string bootstrapServers = "localhost:9092";
            string schemaRegistryUrl = "http://localhost:8083";
            string topicName = "psm_prolucid";

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = bootstrapServers
            };

            var schemaRegistryConfig = new SchemaRegistryConfig
            {
                // Note: you can specify more than one schema registry url using the
                // schema.registry.url property for redundancy (comma separated list). 
                // The property name is not plural to follow the convention set by
                // the Java implementation.
                Url = schemaRegistryUrl
            };

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = "avro-specific-example-group",
                AutoOffsetReset = AutoOffsetReset.Latest,
              
            };

            var avroSerializerConfig = new AvroSerializerConfig
            {
                // optional Avro serializer properties:
                BufferBytes = 100
            };

            int counter_psm = 0;
            int counter_ms2 = 0;
            includedScanNum = new ConcurrentDictionary<int , bool>();

            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationTokenSource cts_paserControl = new CancellationTokenSource();
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
                    Console.WriteLine("Consumer connecting to kafka broker, topic: paser_control");
                    try
                    {
                        while (true)
                        {
                            try
                            {
                                var consumeResult = consumer.Consume(cts.Token);
                                var paserControlMessage = consumeResult.Message.Value;
                                if(paserControlMessage.control_type == 0)
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
            if(connectionMode == BrukerConnectionEnum.ProLucidConnectionOnly)
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
                    Thread.Sleep(3000);
                    var tp = consumer.Assignment;
                    tp= consumer.Assignment;
                    var tpOffset = tp.Select(c => new TopicPartitionOffset(c, new Offset(
                        consumer.GetWatermarkOffsets(c).High.Value + 1))).ToList();
                        
                    //var tpOffset = tp.Select(c => new TopicPartitionOffset(c, Offset.End+1));
                    consumer.Assign(tpOffset );
                    Thread.Sleep(3000);
                    Console.WriteLine("Consumer connecting to kafka broker, topic {0}",topicName);
                    Console.WriteLine("Invoking command line util to start acquisition simulator");
                    CommandLineProcessingUtil.RunBrukerAcquisitionSimulator();
                    try
                    {
                        
                        while (true)
                        {
                            try
                            {
                                var consumeResult = consumer.Consume(cts.Token);
                                var psm = consumeResult.Message.Value;
                                
                                Console.WriteLine($"key: {consumeResult.Message.Key}, psm- ms2_id: {psm.ms2_id}, rt: {psm.rt}, mono mz: {psm.mono_mz},charge: {psm.charge}, " +
                                    $"parent_id: {psm.parent_id}");
                                Console.WriteLine("Offset {0}", consumeResult.Offset);
                               
                                bool scanIncluded = false;
                                //while (true)
                                //{
                                //    if ( includedScanNum.TryGetValue((psm.ms2_id), out scanIncluded))
                                //    {
                                //        //if this ms2 scan is  processed 
                                //        break;
                                //    }
                                //}
                                //if (!scanIncluded)
                                //{
                                //    continue;
                                //}
                                

                                IDs id = IDs.getIDsFromPSMProlucid(psm);// maybe will need to change the rt unit to min
                                
                                counter_psm++;
                                psmReceived = 1;
                                if (!runWithoutExclusionProfile)
                                {
                                    exclusionProfile.evaluateIdentificationAndUpdateCurrentTime(id);
                                }
                                else
                                {
                                    if (id != null)
                                    {
                                        //psmTracker.Add(id);
                                        sw.WriteLine(String.Join(separator: "\t",
                                          id.getScanNum(), id.getPeptideSequence(), id.getPeptideSequence_withModification(),id.getScanTime(),
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
                consumeTask_Prolucid.Wait(600000000);
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
                    Thread.Sleep(2000);
                    var tp = consumer.Assignment;
                    tp = consumer.Assignment;
                    var tpOffset = tp.Select(c => new TopicPartitionOffset(c, new Offset(
                        consumer.GetWatermarkOffsets(c).High.Value))).ToList();

                    //var tpOffset = tp.Select(c => new TopicPartitionOffset(c, Offset.End+1));
                    consumer.Assign(tpOffset);
                    Thread.Sleep(2000);
                    Console.WriteLine("Consumer connecting to kafka broker, topic {0}", ms2TopicName);
                    Console.WriteLine("Invoking command line util to start acquisition simulator");
                    CommandLineProcessingUtil.RunBrukerAcquisitionSimulator();

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
                                if(counter_ms2% GlobalVar.ScansPerOutput == 0)
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
                        Console.WriteLine("Operation cancelled - kafka - {0} consumption",ms2TopicName);
                        consumer.Close();
                    }
                }
            });
                consumeTask_ms2.Wait(600000000);
            }

            consumeTask_paserControl.Wait(600000000);

            cts.Cancel();
            Console.WriteLine("Operation finished");
            Console.WriteLine("Received total {0} psms and {1} ms2", counter_psm, counter_ms2);
            int numScanIncluded = includedScanNum.Values.Where(x => x==true).Count();
            int numScanExcluded = includedScanNum.Values.Where(x => x==false).Count();
            Console.WriteLine("Scan included {0} and excluded {1}", numScanIncluded, numScanExcluded);
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


        //a unit test function that connects to Kafka broker and prints all psms to a file
        static StreamWriter sw;
        public static void PrintAllProlucidPSM()
        {
            psmTracker = new List<IDs>();
            sw = new StreamWriter(
                Path.Combine(InputFileOrganizer.OutputFolderOfTheRun,"ProlucidPSMs.tsv"));
            sw.WriteLine(String.Join(separator: "\t","ms2_id", "peptide_stripped","peptide_modified", "rt(sec)","calc_mass","xcorr","dCN","accessions"));
            Connect(null, BrukerConnectionEnum.ProLucidConnectionOnly, true);
            //foreach(IDs id in psmTracker)
            //{
            //    sw.WriteLine(String.Join(separator: "\t",
            //        id.getScanNum(), id.getPeptideSequence(),id.getScanTime(), 
            //        id.getPeptideMass(), id.getXCorr(), id.getDeltaCN()));
            //}
            sw.Close();
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
