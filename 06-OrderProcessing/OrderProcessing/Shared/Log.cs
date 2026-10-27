using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1
{
    public static class Log
    {
        // قفل لمنع تداخل طباعة الألوان بين الـ Threads المختلفة
        private static readonly object _consoleLock = new object();

        public static void Info(string source, string message)
        {
            WriteLog(source, message, ConsoleColor.Gray);
        }

        public static void Success(string source, string message)
        {
            WriteLog(source, message, ConsoleColor.Green);
        }

        public static void Error(string source, string message)
        {
            WriteLog(source, message, ConsoleColor.Red);
        }

        private static void WriteLog(string source, string message, ConsoleColor color)
        {
            // [HH:mm:ss.fff] التنسيق المطلوب بالظبط
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            int threadId = Environment.CurrentManagedThreadId;
            string formattedLine = $"[{timestamp}] [Thread {threadId}] {source} -> {message}";

            // حماية طباعة الكونسول من تداخل الألوان
            lock (_consoleLock)
            {
                ConsoleColor previousColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.WriteLine(formattedLine);
                Console.ForegroundColor = previousColor;
            }
        }
    }
}
