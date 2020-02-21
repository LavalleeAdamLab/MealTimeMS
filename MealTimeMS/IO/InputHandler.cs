using System;
using System.Threading;
using MealTimeMS.RunTime;
namespace MealTimeMS
{

	//used to handle user input in runtime, currently abandoned
    public static class InputHandler
    {

        static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        static bool running = true;
        public static void ReadConsoleInput()
        {
            
            while (running)
            {
                
                String input= Console.ReadKey().KeyChar.ToString();
                HandleInput(input);   
            }

        }

        private static void HandleInput(String input)
        {
            if (input.Equals("0"))
            {
                Console.WriteLine("Setting output level to detailed");
                DataProcessor.SetOutputLevel(0);
            } else if (input.Equals("1")) {
                Console.WriteLine("Setting output level to simple");
                DataProcessor.SetOutputLevel(1);
            } else if (input.Equals("2")){
                Console.WriteLine("Setting output level to peptide only");
                DataProcessor.SetOutputLevel(2);

            }
            else if (input.Equals("3"))
            {
                Console.WriteLine("Displaying only info headers");
                DataProcessor.SetOutputLevel(3);

            }
            else if (input.Equals("q")){
                Console.WriteLine("Quitting listening");
                ExclusionExplorer.EndMealTimeMS();
            }

        }

        public static void StopRunning()
        {
            running = false;
        }
    }
}
