using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
namespace MealTimeMS
{
    public static class SerializeClone
    {
        public static T DeepClone<T>( T obj)
        {
            using (MemoryStream memory_stream = new MemoryStream())
            {
                // Serialize the object into the memory stream.
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(memory_stream, obj);

                // Rewind the stream and use it to create a new object.
                memory_stream.Position = 0;
                return (T)formatter.Deserialize(memory_stream);
            }
        }
    }
}
