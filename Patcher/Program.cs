using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=================================================");
        Console.WriteLine("        Codex Trainer Patcher v2.0            ");
        Console.WriteLine("=================================================");
        Console.WriteLine("[Codex Patcher] Starting patcher...");

        // 1. Get path to AutoAttackTrainer.dll (same folder as Patcher.exe)
        string currentDir = AppDomain.CurrentDomain.BaseDirectory;
        string trainerSrcPath = Path.Combine(currentDir, "AutoAttackTrainer.dll");

        if (!File.Exists(trainerSrcPath))
        {
            // If running in development environment, fall back to project build directory
            string devPath = Path.GetFullPath(Path.Combine(currentDir, "..", "..", "..", "..", "AutoAttackTrainer", "bin", "Debug", "netstandard2.1", "AutoAttackTrainer.dll"));
            if (File.Exists(devPath))
            {
                trainerSrcPath = devPath;
            }
            else
            {
                Console.WriteLine($"[Error] AutoAttackTrainer.dll not found in the patcher folder.");
                Console.WriteLine($"Please place AutoAttackTrainer.dll in the same folder as this patcher.");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }
        }

        // 2. Find AdventureQuest Worlds Infinity 'Managed' folder
        string gameManagedDir = "";

        // Strategy A: Check if Patcher.exe is placed directly in the 'Managed' folder
        if (File.Exists(Path.Combine(currentDir, "Assembly-CSharp.dll")))
        {
            gameManagedDir = currentDir;
        }
        else
        {
            // Strategy B: Check standard Steam paths
            string standardSteamPath = @"C:\Program Files (x86)\Steam\steamapps\common\AdventureQuest Worlds Unity Playtest\AdventureQuest Worlds Infinity_Data\Managed";
            if (Directory.Exists(standardSteamPath) && File.Exists(Path.Combine(standardSteamPath, "Assembly-CSharp.dll")))
            {
                gameManagedDir = standardSteamPath;
            }
            else
            {
                // Strategy C: Prompt the user
                Console.WriteLine("\n[Notice] Could not find the game's 'Managed' folder automatically.");
                Console.WriteLine("Please copy the path to the game's 'Managed' folder and paste it here.");
                Console.WriteLine(@"Example: C:\Program Files (x86)\Steam\steamapps\common\AdventureQuest Worlds Unity Playtest\AdventureQuest Worlds Infinity_Data\Managed");
                Console.Write("\nPath: ");
                
                string input = Console.ReadLine()?.Trim('"', ' ');
                if (!string.IsNullOrEmpty(input) && Directory.Exists(input) && File.Exists(Path.Combine(input, "Assembly-CSharp.dll")))
                {
                    gameManagedDir = input;
                }
                else
                {
                    Console.WriteLine("\n[Error] Invalid folder or Assembly-CSharp.dll is missing in that folder.");
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                    return;
                }
            }
        }

        Console.WriteLine($"\n[Codex Patcher] Target directory set to: {gameManagedDir}");
        string targetDllPath = Path.Combine(gameManagedDir, "Assembly-CSharp.dll");
        string backupDllPath = Path.Combine(gameManagedDir, "Assembly-CSharp.dll.bak");
        string trainerDestPath = Path.Combine(gameManagedDir, "AutoAttackTrainer.dll");
        string harmonySrcPath = Path.Combine(Path.GetDirectoryName(trainerSrcPath), "0Harmony.dll");
        string harmonyDestPath = Path.Combine(gameManagedDir, "0Harmony.dll");

        try
        {
            // 3. Copy AutoAttackTrainer.dll to target directory
            Console.WriteLine($"[Codex Patcher] Deploying AutoAttackTrainer.dll to game folder...");
            File.Copy(trainerSrcPath, trainerDestPath, true);

            if (File.Exists(harmonySrcPath))
            {
                Console.WriteLine($"[Codex Patcher] Deploying 0Harmony.dll to game folder...");
                File.Copy(harmonySrcPath, harmonyDestPath, true);
            }

            var readerParameters = new ReaderParameters
            {
                AssemblyResolver = new DefaultAssemblyResolver()
            };
            ((DefaultAssemblyResolver)readerParameters.AssemblyResolver).AddSearchDirectory(gameManagedDir);

            // 4. Check if current Assembly-CSharp.dll already has the hook
            bool alreadyInjected = false;
            if (File.Exists(targetDllPath))
            {
                using (var checkAssembly = AssemblyDefinition.ReadAssembly(targetDllPath, readerParameters))
                {
                    var checkModule = checkAssembly.MainModule;
                    var mainType = checkModule.Types.FirstOrDefault(t => t.FullName == "Main");
                    if (mainType != null)
                    {
                        var startMethod = mainType.Methods.FirstOrDefault(m => m.Name == "Start");
                        if (startMethod != null)
                        {
                            alreadyInjected = startMethod.Body.Instructions.Any(inst =>
                                inst.OpCode == OpCodes.Ldtoken &&
                                inst.Operand is TypeReference tr &&
                                tr.Name == "AutoAttackTrainer"
                            );
                        }
                    }
                }
            }

            string sourceDllToPatch;
            if (!alreadyInjected)
            {
                Console.WriteLine($"[Codex Patcher] Target is clean. Creating/updating backup at: {backupDllPath}");
                File.Copy(targetDllPath, backupDllPath, true);
                sourceDllToPatch = backupDllPath;
            }
            else
            {
                Console.WriteLine($"[Codex Patcher] Target is already patched. Re-applying patch using backup: {backupDllPath}");
                if (!File.Exists(backupDllPath))
                {
                    Console.WriteLine("[Error] Backup file is missing but target DLL is already patched.");
                    Console.WriteLine("Please verify game files in Steam first to restore original files.");
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                    return;
                }
                sourceDllToPatch = backupDllPath;
            }

            // Always read from the clean backup to ensure a clean slate and avoid duplicate hooks
            Console.WriteLine($"[Codex Patcher] Reading clean assembly from: {sourceDllToPatch}");
            using (var assemblyCSharp = AssemblyDefinition.ReadAssembly(sourceDllToPatch, readerParameters))
            {
                var module = assemblyCSharp.MainModule;

                // 5. Import UnityEngine.CoreModule references
                Console.WriteLine("[Codex Patcher] Importing UnityEngine.CoreModule references...");
                string coreModulePath = Path.Combine(gameManagedDir, "UnityEngine.CoreModule.dll");
                using (var coreModuleAssembly = AssemblyDefinition.ReadAssembly(coreModulePath, readerParameters))
                {
                    var gameObjectType = coreModuleAssembly.MainModule.Types.First(t => t.FullName == "UnityEngine.GameObject");
                    var addComponentMethod = gameObjectType.Methods.First(m => m.Name == "AddComponent" && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.Name == "Type");
                    var addComponentRef = module.ImportReference(addComponentMethod);

                    var componentType = coreModuleAssembly.MainModule.Types.First(t => t.FullName == "UnityEngine.Component");
                    var getGameObjectMethod = componentType.Methods.First(m => m.Name == "get_gameObject");
                    var getGameObjectRef = module.ImportReference(getGameObjectMethod);

                    // 6. Import System.Type from mscorlib/netstandard
                    Console.WriteLine("[Codex Patcher] Importing System.Type reference...");
                    string corlibPath = Path.Combine(gameManagedDir, "mscorlib.dll");
                    if (!File.Exists(corlibPath))
                    {
                        corlibPath = Path.Combine(gameManagedDir, "netstandard.dll");
                    }
                    using (var corlibAssembly = AssemblyDefinition.ReadAssembly(corlibPath, readerParameters))
                    {
                        var typeDef = corlibAssembly.MainModule.Types.First(t => t.FullName == "System.Type");
                        var getTypeFromHandleMethod = typeDef.Methods.First(m => m.Name == "GetTypeFromHandle" && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.Name == "RuntimeTypeHandle");
                        var getTypeFromHandleRef = module.ImportReference(getTypeFromHandleMethod);

                        // 7. Import AutoAttackTrainer references
                        Console.WriteLine("[Codex Patcher] Importing AutoAttackTrainer references...");
                        using (var trainerAssembly = AssemblyDefinition.ReadAssembly(trainerDestPath, readerParameters))
                        {
                            var trainerType = trainerAssembly.MainModule.Types.First(t => t.FullName == "AutoAttackTrainer");
                            var trainerTypeRef = module.ImportReference(trainerType);

                            // 8. Hook Main.Start()
                            Console.WriteLine("[Codex Patcher] Patching Main.Start()...");
                            var mainType = module.Types.First(t => t.FullName == "Main");
                            var startMethod = mainType.Methods.First(m => m.Name == "Start");

                            bool isAlreadyInjected = startMethod.Body.Instructions.Any(inst => inst.OpCode == OpCodes.Ldtoken && inst.Operand is TypeReference tr && tr.Name == "AutoAttackTrainer");
                            if (!isAlreadyInjected)
                            {
                                var il = startMethod.Body.GetILProcessor();
                                var firstInst = startMethod.Body.Instructions[0];

                                var instructions = new[]
                                {
                                    il.Create(OpCodes.Ldarg_0),
                                    il.Create(OpCodes.Call, getGameObjectRef),
                                    il.Create(OpCodes.Ldtoken, trainerTypeRef),
                                    il.Create(OpCodes.Call, getTypeFromHandleRef),
                                    il.Create(OpCodes.Callvirt, addComponentRef),
                                    il.Create(OpCodes.Pop)
                                };

                                foreach (var inst in instructions)
                                {
                                    il.InsertBefore(firstInst, inst);
                                }
                                Console.WriteLine("[Codex Patcher] Main.Start() patched successfully!");
                            }
                            else
                            {
                                Console.WriteLine("[Codex Patcher] Main.Start() already contains AutoAttackTrainer hook.");
                            }
                        }
                    }
                }

                // 9. Write modified Assembly-CSharp.dll back to target directory
                Console.WriteLine($"[Codex Patcher] Saving patched Assembly-CSharp.dll to: {targetDllPath}");
                assemblyCSharp.Write(targetDllPath);
            }

            Console.WriteLine("\n[Codex Patcher] Success! Game patched and deployed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[Codex Patcher] Fatal Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}
