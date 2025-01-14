using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace ChatClient.Video
{
    class SerilizeDeserialize
    {
        //--------------------------------------------
        //--------------------------------------------
        // Serialize collection of any type to a byte stream

        public static byte[] Serialize<T>(T obj)
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                BinaryFormatter binSerializer = new BinaryFormatter();
                binSerializer.Serialize(memStream, obj);
                return memStream.ToArray();
            }
        }
        //--------------------------------------------
        // DSerialize collection of any type to a byte stream

        public static T Deserialize<T>(byte[] serializedObj)
        {
            T obj = default(T);
            using (MemoryStream memStream = new MemoryStream(serializedObj))
            {
                BinaryFormatter binSerializer = new BinaryFormatter();
                obj = (T)binSerializer.Deserialize(memStream);
            }
            return obj;
        }
        //--------------------------------------------
        void Ispolzovanie()
        {
            ArrayList arrayListMem = new ArrayList() { "One", "Two", "Three", "Four", "Five", "Six", "Seven" };
            Console.WriteLine("Serializing to Memory : arrayListMem");
            byte[] stream = SerilizeDeserialize.Serialize(arrayListMem);

            ArrayList arrayListMemDes = new ArrayList();

            arrayListMemDes = SerilizeDeserialize.Deserialize<ArrayList>(stream);

            Console.WriteLine("DSerializing From Memory : arrayListMemDes");
            foreach (var item in arrayListMemDes)
            {
                Console.WriteLine(item);
            }
        }
        //--------------------------------------------
    }
}
