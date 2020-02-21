using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace MealTimeMS.Tester
{
    class ReadLessLines
    {
        public static void DoJob(String fileIn, String fileOut, int start, int num)
        {
            WriterClass.initiateWriter(fileOut);
            StreamReader reader = new StreamReader(fileIn);
            WriterClass.writeln(reader.ReadLine());
            for (int i = 0; i < start-1; i++)
            {
                reader.ReadLine();
            }
            for(int i = 0; i < num; i++)
            {
                WriterClass.writeln(reader.ReadLine());
            }

            WriterClass.CloseWriter();

        }

    }
}
