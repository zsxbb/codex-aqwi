using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=================================================");
        Console.WriteLine("        Codex Activation Key Generator        ");
        Console.WriteLine("=================================================");
        Console.WriteLine("Secret Salt: codex_secret_2026");
        Console.WriteLine();

        while (true)
        {
            Console.Write("Enter username (or type 'exit' to quit): ");
            string username = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(username))
            {
                Console.WriteLine("Username cannot be empty!\n");
                continue;
            }

            if (username.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            string key = GenerateKey(username);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Generated Key for '{username}': {key}");
            Console.ResetColor();
            Console.WriteLine();
        }
    }

    private static string GenerateKey(string username)
    {
        if (string.IsNullOrEmpty(username)) return "00000";

        // Must match the trainer's logic exactly: lowercase, trim, and salt
        string input = username.Trim().ToLower() + "codex_secret_2026";
        
        // FNV-1a 32-bit hash algorithm
        uint hash = 2166136261U;
        foreach (char c in input)
        {
            hash ^= (uint)c;
            hash *= 16777619U;
        }

        // Format to a 5-digit code (10000 - 99999)
        uint code = (hash % 90000U) + 10000U;
        return code.ToString();
    }
}
