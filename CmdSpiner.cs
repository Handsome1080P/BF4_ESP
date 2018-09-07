using System;

namespace PZ_BF4
{
    class ConsoleSpiner
    {
        int counter;
        public ConsoleSpiner()
        {
            counter = 0;
        }
        public void Turn()
        {
            counter++;
            switch (counter % 4)
            {
                case 0: Console.Write("1"); break;
                case 1: Console.Write("0"); break;
                case 2: Console.Write("1"); break;
                case 3: Console.Write("0"); break;
            }
            Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
            Console.CursorVisible = false;
        }
    }
}
