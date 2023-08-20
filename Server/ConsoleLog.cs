using System;

namespace Final_Aisle_Server
{
    /// <summary>
    /// Utility class responsible for formatting console outputs.
    /// </summary>
    public static class ConsoleLog
    {
        /// <summary>
        /// For printing messages related to the major events in the status of the server (e.g. starting)
        /// </summary>
        public static void WriteBigStatus(string message) => PrintWithTimestamp(ConsoleColor.Green, "Server", message);
        
        /// <summary>
        /// For printing messages related to the minor events in the status of the server (e.g. generating keys)
        /// </summary>
        public static void WriteSmallStatus(string message) => PrintWithTimestamp(ConsoleColor.White, "Server", message);

        /// <summary>
        /// For printing messages related to significant errors (need attention)
        /// </summary>
        public static void WriteBigError(string message) => PrintWithTimestamp(ConsoleColor.Red, "Error", message);

        /// <summary>
        /// For printing messages related to minor errors
        /// </summary>
        public static void WriteSmallError(string message) => PrintWithTimestamp(ConsoleColor.White, "Error", message);

        /// <summary>
        /// For printing major messages related to connections and clients (e.g. player joining or player leaving)
        /// </summary>
        public static void WriteBigIO(string message) => PrintWithTimestamp(ConsoleColor.Yellow, "I/O", message);

        /// <summary>
        /// For printing minor messages related to connections and clients (e.g. player moved a certain way, or to a certain place)
        /// </summary>
        public static void WriteSmallIO(string message) => PrintWithTimestamp(ConsoleColor.White, "I/O", message);

        /// <summary>
        /// For printing major messages related to data transfer
        /// </summary>
        public static void WriteBigData(string message) => PrintWithTimestamp(ConsoleColor.Cyan, "Data", message);

        /// <summary>
        /// For printing minor messages related to data transfer
        /// </summary>
        public static void WriteSmallData(string message) => PrintWithTimestamp(ConsoleColor.White, "Data", message);

        /// <summary>
        /// For printing messages related to command information
        /// </summary>
        public static void WriteInfo(string message) => PrintWithTimestamp(ConsoleColor.White, "Info", message);

        /// <summary>
        /// Prints a message prefixed with the timestamp.
        /// </summary>
        private static void PrintWithTimestamp(ConsoleColor colour, string prefix, string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("[" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + "]");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("[" + prefix + "]: ");
            Console.ForegroundColor = colour;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
