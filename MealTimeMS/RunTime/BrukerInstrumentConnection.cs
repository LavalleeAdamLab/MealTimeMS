using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

namespace MealTimeMS.RunTime
{
    class BrukerInstrumentConnection
    {
        public static void DoJob()
        {


        }
        public static void Connect(ExclusionProfile exclusionProfile)
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
                GroupId = "avro-specific-example-group"
            };

            var avroSerializerConfig = new AvroSerializerConfig
            {
                // optional Avro serializer properties:
                BufferBytes = 100
            };

            CancellationTokenSource cts = new CancellationTokenSource();
            var consumeTask = Task.Run(() =>
            {
                using (var schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig))
                using (var consumer =
                    new ConsumerBuilder<string, PsmProlucid>(consumerConfig)
                       .SetValueDeserializer(new AvroDeserializer<PsmProlucid>(schemaRegistry, null).AsSyncOverAsync())
                        .SetErrorHandler((_, e) => Console.WriteLine($"Error: {e.Reason}"))
                        .Build())
                {
                    consumer.Subscribe(topicName);
                    Console.WriteLine("Consumer connecting to kafka broker");
                    try
                    {
                        while (true)
                        {
                            try
                            {
                                var consumeResult = consumer.Consume(cts.Token);
                                var psm = consumeResult.Message.Value;

                                Console.WriteLine($"key: {consumeResult.Message.Key}, mono mz: {psm.mono_mz},charge: {psm.charge}, " +
                                    $" ms2id: {psm.ms2_id}, parent_id: {psm.parent_id}");
                               
                                IDs id = IDs.getIDsFromPSMProlucid(psm);
                                //exclusionProfile.evaluateIdentification_public_access_wrapper(id);
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
                                    Console.WriteLine("End-of-acquisition message received from Paser-control. Stopping Mealtime-MS");
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
            consumeTask_paserControl.Wait(600000);
            consumeTask.Wait(600000);

            cts.Cancel();
            Console.WriteLine("Operation finished");
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

    }
}
