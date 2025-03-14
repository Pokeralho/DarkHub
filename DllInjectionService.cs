using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace DarkHub
{
    public class DllInjectionService
    {
        #region Native Structures

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGE_DOS_HEADER
        {
            public ushort e_magic;
            public ushort e_cblp;
            public ushort e_cp;
            public ushort e_crlc;
            public ushort e_cparhdr;
            public ushort e_minalloc;
            public ushort e_maxalloc;
            public ushort e_ss;
            public ushort e_sp;
            public ushort e_csum;
            public ushort e_ip;
            public ushort e_cs;
            public ushort e_lfarlc;
            public ushort e_ovno;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public ushort[] e_res1;

            public ushort e_oemid;
            public ushort e_oeminfo;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public ushort[] e_res2;

            public int e_lfanew;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGE_NT_HEADERS
        {
            public uint Signature;
            public IMAGE_FILE_HEADER FileHeader;
            public IMAGE_OPTIONAL_HEADER32 OptionalHeader32;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGE_FILE_HEADER
        {
            public ushort Machine;
            public ushort NumberOfSections;
            public uint TimeDateStamp;
            public uint PointerToSymbolTable;
            public uint NumberOfSymbols;
            public ushort SizeOfOptionalHeader;
            public ushort Characteristics;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGE_OPTIONAL_HEADER32
        {
            public ushort Magic;
            public byte MajorLinkerVersion;
            public byte MinorLinkerVersion;
            public uint SizeOfCode;
            public uint SizeOfInitializedData;
            public uint SizeOfUninitializedData;
            public uint AddressOfEntryPoint;
            public uint BaseOfCode;
            public uint BaseOfData;
            public uint ImageBase;
            public uint SectionAlignment;
            public uint FileAlignment;
            public ushort MajorOperatingSystemVersion;
            public ushort MinorOperatingSystemVersion;
            public ushort MajorImageVersion;
            public ushort MinorImageVersion;
            public ushort MajorSubsystemVersion;
            public ushort MinorSubsystemVersion;
            public uint Win32VersionValue;
            public uint SizeOfImage;
            public uint SizeOfHeaders;
            public uint CheckSum;
            public ushort Subsystem;
            public ushort DllCharacteristics;
            public uint SizeOfStackReserve;
            public uint SizeOfStackCommit;
            public uint SizeOfHeapReserve;
            public uint SizeOfHeapCommit;
            public uint LoaderFlags;
            public uint NumberOfRvaAndSizes;
            public IMAGE_DATA_DIRECTORY ExportTable;
            public IMAGE_DATA_DIRECTORY ImportTable;
            public IMAGE_DATA_DIRECTORY ResourceTable;
            public IMAGE_DATA_DIRECTORY ExceptionTable;
            public IMAGE_DATA_DIRECTORY CertificateTable;
            public IMAGE_DATA_DIRECTORY BaseRelocationTable;
            public IMAGE_DATA_DIRECTORY Debug;
            public IMAGE_DATA_DIRECTORY Architecture;
            public IMAGE_DATA_DIRECTORY GlobalPtr;
            public IMAGE_DATA_DIRECTORY TLSTable;
            public IMAGE_DATA_DIRECTORY LoadConfigTable;
            public IMAGE_DATA_DIRECTORY BoundImport;
            public IMAGE_DATA_DIRECTORY IAT;
            public IMAGE_DATA_DIRECTORY DelayImportDescriptor;
            public IMAGE_DATA_DIRECTORY CLRRuntimeHeader;
            public IMAGE_DATA_DIRECTORY Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGE_SECTION_HEADER
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] Name;

            public uint VirtualSize;
            public uint VirtualAddress;
            public uint SizeOfRawData;
            public uint PointerToRawData;
            public uint PointerToRelocations;
            public uint PointerToLinenumbers;
            public ushort NumberOfRelocations;
            public ushort NumberOfLinenumbers;
            public uint Characteristics;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGE_DATA_DIRECTORY
        {
            public int VirtualAddress;
            public int Size;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGE_EXPORT_DIRECTORY
        {
            public uint Characteristics;
            public uint TimeDateStamp;
            public ushort MajorVersion;
            public ushort MinorVersion;
            public uint Name;
            public uint Base;
            public uint NumberOfFunctions;
            public uint NumberOfNames;
            public uint AddressOfFunctions;
            public uint AddressOfNames;
            public uint AddressOfNameOrdinals;
        }

        #endregion Native Structures

        #region Native Methods

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetLastError();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool TerminateThread(IntPtr handle, uint exitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetExitCodeThread(IntPtr hThread, out uint lpExitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint dwFreeType);

        #endregion Native Methods

        private const int PROCESS_ALL_ACCESS = 0x1F0FFF;
        private const uint MEM_COMMIT = 0x1000;
        private const uint MEM_RESERVE = 0x2000;
        private const uint PAGE_EXECUTE_READWRITE = 0x40;
        private const uint PAGE_READWRITE = 0x04;

        #region Anti-Debug e Anti-VM

        [DllImport("kernel32.dll")]
        private static extern bool IsDebuggerPresent();

        [DllImport("kernel32.dll")]
        private static extern bool CheckRemoteDebuggerPresent(IntPtr hProcess, ref bool isDebuggerPresent);

        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref int processInformation, int processInformationLength, ref int returnLength);

        private bool IsBeingDebugged()
        {
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    var sw = Stopwatch.StartNew();
                    Thread.Sleep(1);
                    sw.Stop();
                    if (sw.ElapsedMilliseconds > 50)
                        return true;
                }

                if (CheckPebDebugFlags())
                    return true;

                if (CheckHardwareBreakpoints())
                    return true;

                if (CheckDebuggerViaException())
                    return true;

                if (CheckDebuggerWindows())
                    return true;

                return false;
            }
            catch
            {
                return true;
            }
        }

        private bool CheckPebDebugFlags()
        {
            try
            {
                int isDebugged = 0;
                int returnLength = 0;

                if (NtQueryInformationProcess(Process.GetCurrentProcess().Handle, 7, ref isDebugged, sizeof(int), ref returnLength) == 0)
                {
                    if (isDebugged != 0)
                        return true;
                }

                if (NtQueryInformationProcess(Process.GetCurrentProcess().Handle, 0x7, ref isDebugged, sizeof(int), ref returnLength) == 0)
                {
                    if (isDebugged != 0)
                        return true;
                }

                return false;
            }
            catch
            {
                return true;
            }
        }

        private bool CheckHardwareBreakpoints()
        {
            try
            {
                var context = new CONTEXT();
                context.ContextFlags = CONTEXT_DEBUG_REGISTERS;

                IntPtr thread = GetCurrentThread();
                if (GetThreadContext(thread, ref context))
                {
                    if (context.Dr0 != 0 || context.Dr1 != 0 || context.Dr2 != 0 || context.Dr3 != 0)
                        return true;
                }
                return false;
            }
            catch
            {
                return true;
            }
        }

        private bool CheckDebuggerViaException()
        {
            try
            {
                SetLastError(0);
                OutputDebugString("AntiDebugCheck");
                if (GetLastError() != 0)
                    return true;

                return false;
            }
            catch
            {
                return true;
            }
        }

        private bool CheckDebuggerWindows()
        {
            string[] debuggerWindows = {
                "OLLYDBG", "WinDbgFrameClass", "ID", "Visual Studio",
                "IDA", "IDA Pro", "Immunity Debugger", "x64dbg", "x32dbg"
            };

            try
            {
                foreach (var window in debuggerWindows)
                {
                    if (FindWindow(window, null) != IntPtr.Zero)
                        return true;
                }
                return false;
            }
            catch
            {
                return true;
            }
        }

        private bool IsVirtualMachine()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("Select * from Win32_ComputerSystem"))
                {
                    foreach (var item in searcher.Get())
                    {
                        string manufacturer = item["Manufacturer"].ToString().ToLower();
                        string model = item["Model"].ToString().ToLower();

                        if (manufacturer.Contains("vmware") ||
                            manufacturer.Contains("virtualbox") ||
                            manufacturer.Contains("microsoft corporation") && model.Contains("virtual") ||
                            manufacturer.Contains("qemu"))
                            return true;
                    }
                }

                var memory = new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory;
                if (memory < 4L * 1024 * 1024 * 1024)
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
        }

        #endregion Anti-Debug e Anti-VM

        #region String Obfuscation

        private string ObfuscateString(string input)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(bytes[i] ^ 0x7A);
            }
            return Convert.ToBase64String(bytes);
        }

        private string DeobfuscateString(string input)
        {
            byte[] bytes = Convert.FromBase64String(input);
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(bytes[i] ^ 0x7A);
            }
            return Encoding.UTF8.GetString(bytes);
        }

        private readonly Dictionary<string, string> _obfuscatedStrings;

        public DllInjectionService()
        {
            _obfuscatedStrings = new Dictionary<string, string>
            {
                { "kernel32.dll", ObfuscateString("kernel32.dll") },
                { "ntdll.dll", ObfuscateString("ntdll.dll") },
                { "VirtualAllocEx", ObfuscateString("VirtualAllocEx") },
                { "WriteProcessMemory", ObfuscateString("WriteProcessMemory") },
                { "CreateRemoteThread", ObfuscateString("CreateRemoteThread") }
            };
        }

        #endregion String Obfuscation

        #region Encryption

        private byte[] EncryptDll(byte[] dllBytes, byte[] key = null)
        {
            using (Aes aes = Aes.Create())
            {
                if (key == null)
                {
                    aes.GenerateKey();
                    key = aes.Key;
                }

                aes.Key = key;
                aes.GenerateIV();

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    msEncrypt.Write(aes.IV, 0, aes.IV.Length);

                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    using (BinaryWriter swEncrypt = new BinaryWriter(csEncrypt))
                    {
                        swEncrypt.Write(dllBytes);
                    }

                    return msEncrypt.ToArray();
                }
            }
        }

        private byte[] DecryptDll(byte[] encryptedBytes, byte[] key)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;

                byte[] iv = new byte[aes.IV.Length];
                Array.Copy(encryptedBytes, 0, iv, 0, iv.Length);
                aes.IV = iv;

                using (MemoryStream msDecrypt = new MemoryStream())
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    using (BinaryWriter swDecrypt = new BinaryWriter(csDecrypt))
                    {
                        swDecrypt.Write(encryptedBytes, iv.Length, encryptedBytes.Length - iv.Length);
                    }

                    return msDecrypt.ToArray();
                }
            }
        }

        #endregion Encryption

        #region Cleanup

        private void CleanupPEHeaders(IntPtr processHandle, IntPtr baseAddress)
        {
            try
            {
                byte[] zeros = new byte[4096];
                UIntPtr bytesWritten;
                WriteProcessMemory(processHandle, baseAddress, zeros, (uint)zeros.Length, out bytesWritten);
            }
            catch { }
        }

        private void RemoveLoadedModuleEntry(IntPtr processHandle)
        {
            try
            {
            }
            catch { }
        }

        #endregion Cleanup

        public string LastError { get; private set; } = "";

        #region API Ofuscação

        private delegate IntPtr OpenProcessDelegate(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        private delegate IntPtr VirtualAllocExDelegate(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        private delegate bool WriteProcessMemoryDelegate(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        private delegate IntPtr CreateRemoteThreadDelegate(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        private Dictionary<string, Delegate> _cachedDelegates = new Dictionary<string, Delegate>();

        private T GetApiDelegate<T>(string dllName, string apiName) where T : Delegate
        {
            string key = $"{dllName}_{apiName}";
            if (_cachedDelegates.ContainsKey(key))
                return (T)_cachedDelegates[key];

            byte[] dllNameBytes = Encoding.ASCII.GetBytes(DeobfuscateString(dllName));
            byte[] apiNameBytes = Encoding.ASCII.GetBytes(DeobfuscateString(apiName));

            IntPtr hModule = GetModuleHandleInternal(dllNameBytes);
            if (hModule == IntPtr.Zero)
                hModule = LoadLibraryInternal(dllNameBytes);

            if (hModule == IntPtr.Zero)
                throw new Exception($"Não foi possível carregar {dllName}");

            IntPtr procAddress = GetProcAddressInternal(hModule, apiNameBytes);
            if (procAddress == IntPtr.Zero)
                throw new Exception($"Não foi possível encontrar {apiName}");

            var result = Marshal.GetDelegateForFunctionPointer<T>(procAddress);
            _cachedDelegates[key] = result;
            return result;
        }

        [DllImport("kernel32.dll", EntryPoint = "LoadLibraryA", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr LoadLibraryInternal(byte[] lpFileName);

        [DllImport("kernel32.dll", EntryPoint = "GetProcAddress", SetLastError = true)]
        private static extern IntPtr GetProcAddressInternal(IntPtr hModule, byte[] lpProcName);

        [DllImport("kernel32.dll", EntryPoint = "GetModuleHandleA", SetLastError = true)]
        private static extern IntPtr GetModuleHandleInternal(byte[] lpModuleName);

        #endregion API Ofuscação

        #region Anti-VM e Anti-Análise

        private bool IsInVirtualEnvironment()
        {
            try
            {
                int detectionCount = 0;

                if (CheckVMwareArtifacts())
                    detectionCount++;

                if (CheckVirtualBoxArtifacts())
                    detectionCount++;

                if (CheckHyperVArtifacts())
                    detectionCount++;

                if (detectionCount > 0 && CheckSystemArtifacts())
                    detectionCount++;

                return detectionCount >= 2;
            }
            catch
            {
                return false;
            }
        }

        private bool CheckVMwareArtifacts()
        {
            try
            {
                string[] vmwareServices = {
                    "VMTools",
                    "Vmhgfs",
                    "VMMEMCTL",
                    "Vmmouse"
                };

                foreach (var service in vmwareServices)
                {
                    if (ServiceExists(service))
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private bool CheckVirtualBoxArtifacts()
        {
            try
            {
                string[] vboxServices = {
                    "VBoxService",
                    "VBoxTray"
                };

                foreach (var service in vboxServices)
                {
                    if (ServiceExists(service))
                        return true;
                }

                return Directory.Exists(@"C:\Windows\System32\drivers\VBox");
            }
            catch
            {
                return false;
            }
        }

        private bool CheckHyperVArtifacts()
        {
            try
            {
                string[] hyperVServices = {
                    "vmicheartbeat",
                    "vmicvss"
                };

                foreach (var service in hyperVServices)
                {
                    if (ServiceExists(service))
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private bool CheckSystemArtifacts()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("Select * from Win32_ComputerSystem"))
                {
                    foreach (var item in searcher.Get())
                    {
                        string manufacturer = item["Manufacturer"].ToString().ToLower();
                        string model = item["Model"].ToString().ToLower();

                        if ((manufacturer.Contains("vmware") && model.Contains("vmware")) ||
                            (manufacturer.Contains("virtualbox") && model.Contains("virtualbox")) ||
                            (manufacturer.Contains("microsoft corporation") && model.Contains("virtual")))
                            return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private bool ServiceExists(string serviceName)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher($"Select * From Win32_Service Where Name = '{serviceName}'"))
                {
                    return searcher.Get().Count > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        #endregion Anti-VM e Anti-Análise

        #region Estruturas e Constantes Adicionais

        [StructLayout(LayoutKind.Sequential)]
        private struct CONTEXT
        {
            public uint ContextFlags;
            public uint Dr0;
            public uint Dr1;
            public uint Dr2;
            public uint Dr3;
            public uint Dr6;
            public uint Dr7;
        }

        private const uint CONTEXT_DEBUG_REGISTERS = 0x00010000;

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentThread();

        [DllImport("kernel32.dll")]
        private static extern bool GetThreadContext(IntPtr hThread, ref CONTEXT lpContext);

        [DllImport("kernel32.dll")]
        private static extern void SetLastError(uint dwErrCode);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
        private static extern void OutputDebugString(string lpOutputString);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        #endregion Estruturas e Constantes Adicionais

        #region Segurança Adicional

        private bool VerifyTrustedEnvironment()
        {
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
                    {
                        LastError = "Necessário executar como administrador";
                        return false;
                    }
                }

                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                if (!VerifyFileSignature(exePath))
                {
                    LastError = "Executável não está assinado digitalmente";
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool VerifyFileSignature(string filePath)
        {
            try
            {
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool IsSystemSecure()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(@"root\SecurityCenter2", "SELECT * FROM AntiVirusProduct"))
                {
                    foreach (var av in searcher.Get())
                    {
                        string avName = av["displayName"].ToString().ToLower();
                        if (avName.Contains("windows defender") || avName.Contains("microsoft defender"))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            catch
            {
                return true;
            }
        }

        #endregion Segurança Adicional

        public bool InjectDll(int processId, string dllPath)
        {
            try
            {
                if (!VerifyBasicSecurity())
                    return false;

                IntPtr kernelBase = GetKernelBase();
                if (kernelBase == IntPtr.Zero)
                {
                    LastError = "Erro ao localizar kernel32";
                    return false;
                }

                IntPtr loadLibraryAddr = GetProcAddressWithHash(kernelBase, 0x726774C);
                if (loadLibraryAddr == IntPtr.Zero)
                {
                    LastError = "Erro ao localizar função necessária";
                    return false;
                }

                const int PROCESS_CREATE_THREAD = 0x0002;
                const int PROCESS_VM_OPERATION = 0x0008;
                const int PROCESS_VM_WRITE = 0x0020;
                const int PROCESS_VM_READ = 0x0010;

                IntPtr processHandle = OpenProcess(
                    PROCESS_CREATE_THREAD | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ,
                    false, processId);

                if (processHandle == IntPtr.Zero)
                {
                    LastError = "Erro ao abrir processo";
                    return false;
                }

                try
                {
                    int pathSize = (dllPath.Length + 1) * sizeof(char);
                    IntPtr allocatedMemory = VirtualAllocEx(processHandle, IntPtr.Zero,
                        (uint)pathSize, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);

                    if (allocatedMemory == IntPtr.Zero)
                    {
                        LastError = "Erro ao alocar memória";
                        return false;
                    }

                    try
                    {
                        byte[] pathBytes = Encoding.ASCII.GetBytes(dllPath + "\0");
                        int chunkSize = 16;

                        for (int i = 0; i < pathBytes.Length; i += chunkSize)
                        {
                            int writeSize = Math.Min(chunkSize, pathBytes.Length - i);
                            byte[] chunk = new byte[writeSize];
                            Array.Copy(pathBytes, i, chunk, 0, writeSize);

                            UIntPtr bytesWritten;
                            if (!WriteProcessMemory(processHandle, IntPtr.Add(allocatedMemory, i),
                                chunk, (uint)writeSize, out bytesWritten))
                            {
                                LastError = "Erro ao escrever memória";
                                return false;
                            }

                            Thread.Sleep(1);
                        }

                        const uint THREAD_CREATE_FLAGS = 0x00000004;
                        IntPtr threadHandle = CreateRemoteThread(processHandle, IntPtr.Zero, 0,
                            loadLibraryAddr, allocatedMemory, THREAD_CREATE_FLAGS, IntPtr.Zero);

                        if (threadHandle == IntPtr.Zero)
                        {
                            LastError = "Erro ao criar thread";
                            return false;
                        }

                        try
                        {
                            Random rnd = new Random();
                            Thread.Sleep(rnd.Next(100, 500));

                            if (ResumeThread(threadHandle) == uint.MaxValue)
                            {
                                LastError = "Erro ao retomar thread";
                                return false;
                            }

                            uint waitResult = WaitForSingleObject(threadHandle, 10000);
                            if (waitResult != 0)
                            {
                                LastError = "Timeout ao aguardar injeção";
                                return false;
                            }

                            uint exitCode = 0;
                            if (!GetExitCodeThread(threadHandle, out exitCode) || exitCode == 0)
                            {
                                LastError = "Erro na injeção";
                                return false;
                            }

                            LastError = "Operação concluída com sucesso";
                            return true;
                        }
                        finally
                        {
                            if (threadHandle != IntPtr.Zero)
                                CloseHandle(threadHandle);
                        }
                    }
                    finally
                    {
                        if (allocatedMemory != IntPtr.Zero)
                            VirtualFreeEx(processHandle, allocatedMemory, 0, 0x8000);
                    }
                }
                finally
                {
                    if (processHandle != IntPtr.Zero)
                        CloseHandle(processHandle);
                }
            }
            catch (Exception ex)
            {
                LastError = $"Erro: {ex.Message}";
                return false;
            }
        }

        private bool VerifyBasicSecurity()
        {
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
                    {
                        LastError = "Necessário executar como administrador";
                        return false;
                    }
                }

                if (IsDebuggerPresent())
                {
                    LastError = "Debugger detectado";
                    return false;
                }

                return true;
            }
            catch
            {
                LastError = "Erro ao verificar segurança básica";
                return false;
            }
        }

        #region Syscalls Diretas

        [StructLayout(LayoutKind.Sequential)]
        private struct OBJECT_ATTRIBUTES
        {
            public int Length;
            public IntPtr RootDirectory;
            public IntPtr ObjectName;
            public uint Attributes;
            public IntPtr SecurityDescriptor;
            public IntPtr SecurityQualityOfService;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CLIENT_ID
        {
            public IntPtr UniqueProcess;
            public IntPtr UniqueThread;
        }

        private delegate int NtOpenProcessDelegate(
            out IntPtr ProcessHandle,
            uint DesiredAccess,
            IntPtr ObjectAttributes,
            IntPtr ClientId);

        private delegate int NtAllocateVirtualMemoryDelegate(
            IntPtr ProcessHandle,
            IntPtr BaseAddress,
            IntPtr ZeroBits,
            uint RegionSize,
            uint AllocationType,
            uint Protect);

        private delegate int NtWriteVirtualMemoryDelegate(
            IntPtr ProcessHandle,
            IntPtr BaseAddress,
            byte[] Buffer,
            uint NumberOfBytesToWrite,
            out uint NumberOfBytesWritten);

        private delegate int NtCreateThreadExDelegate(
            out IntPtr ThreadHandle,
            uint DesiredAccess,
            IntPtr ObjectAttributes,
            IntPtr ProcessHandle,
            IntPtr StartAddress,
            IntPtr Parameter,
            bool CreateSuspended,
            uint StackZeroBits,
            uint SizeOfStackCommit,
            uint SizeOfStackReserve,
            IntPtr ThreadContext);

        #endregion Syscalls Diretas

        private bool MapSections(IntPtr processHandle, byte[] dllBytes, IntPtr baseAddress)
        {
            try
            {
                if (dllBytes == null || dllBytes.Length == 0)
                {
                    LastError = "DLL inválida: array de bytes vazio";
                    return false;
                }

                LastError = $"Tamanho total da DLL: {dllBytes.Length} bytes";

                IMAGE_DOS_HEADER dosHeader = ByteArrayToStructure<IMAGE_DOS_HEADER>(dllBytes);
                if (dosHeader.e_lfanew <= 0 || dosHeader.e_lfanew >= dllBytes.Length)
                {
                    LastError = $"DLL inválida: offset NT header ({dosHeader.e_lfanew}) inválido";
                    return false;
                }

                IMAGE_NT_HEADERS ntHeaders = ByteArrayToStructure<IMAGE_NT_HEADERS>(dllBytes, dosHeader.e_lfanew);
                LastError = $"Número de seções: {ntHeaders.FileHeader.NumberOfSections}";

                if (ntHeaders.OptionalHeader32.SizeOfHeaders > dllBytes.Length)
                {
                    LastError = $"DLL inválida: tamanho dos cabeçalhos ({ntHeaders.OptionalHeader32.SizeOfHeaders}) maior que o arquivo ({dllBytes.Length})";
                    return false;
                }

                UIntPtr bytesWritten;
                if (!WriteProcessMemory(processHandle, baseAddress, dllBytes,
                    ntHeaders.OptionalHeader32.SizeOfHeaders, out bytesWritten))
                {
                    LastError = $"Erro ao escrever cabeçalhos (Erro: {GetLastError()})";
                    return false;
                }

                int sectionOffset = dosHeader.e_lfanew + Marshal.SizeOf<IMAGE_NT_HEADERS>();

                if (sectionOffset >= dllBytes.Length)
                {
                    LastError = $"DLL inválida: offset das seções ({sectionOffset}) fora dos limites";
                    return false;
                }

                uint sectionAlignment = ntHeaders.OptionalHeader32.SectionAlignment;
                if (sectionAlignment == 0) sectionAlignment = 0x1000;

                const int MAX_SECTION_SIZE = 100 * 1024 * 1024;
                byte[] zeroBuffer = new byte[0x1000];

                for (int i = 0; i < ntHeaders.FileHeader.NumberOfSections; i++)
                {
                    int currentSectionOffset = sectionOffset + (i * Marshal.SizeOf<IMAGE_SECTION_HEADER>());
                    if (currentSectionOffset + Marshal.SizeOf<IMAGE_SECTION_HEADER>() > dllBytes.Length)
                    {
                        LastError = $"DLL inválida: seção {i} fora dos limites do arquivo (offset: {currentSectionOffset})";
                        return false;
                    }

                    IMAGE_SECTION_HEADER section = ByteArrayToStructure<IMAGE_SECTION_HEADER>(
                        dllBytes, currentSectionOffset);

                    string sectionName = Encoding.ASCII.GetString(section.Name).TrimEnd('\0');

                    uint virtualSize = section.VirtualSize;
                    if (virtualSize == 0)
                    {
                        virtualSize = section.SizeOfRawData;
                    }
                    if (virtualSize == 0)
                    {
                        virtualSize = sectionAlignment;
                    }

                    virtualSize = ((virtualSize + sectionAlignment - 1) / sectionAlignment) * sectionAlignment;

                    if (virtualSize > MAX_SECTION_SIZE)
                    {
                        LastError = $"Tamanho da seção {i} ({sectionName}) muito grande: 0x{virtualSize:X} bytes";
                        return false;
                    }

                    LastError = $"Processando seção {i} ({sectionName}): " +
                              $"RawSize=0x{section.SizeOfRawData:X}, " +
                              $"VirtualSize=0x{virtualSize:X}, " +
                              $"RawPtr=0x{section.PointerToRawData:X}, " +
                              $"VirtualAddress=0x{section.VirtualAddress:X}";

                    IntPtr sectionAddress = IntPtr.Add(baseAddress, (int)section.VirtualAddress);

                    if (sectionAddress == IntPtr.Zero || sectionAddress.ToInt64() < baseAddress.ToInt64())
                    {
                        LastError = $"Endereço inválido para seção {i} ({sectionName}): 0x{sectionAddress.ToInt64():X}";
                        return false;
                    }

                    uint oldProtect;
                    if (!VirtualProtectEx(processHandle, sectionAddress,
                        new UIntPtr(virtualSize), PAGE_EXECUTE_READWRITE, out oldProtect))
                    {
                        uint error = GetLastError();
                        LastError = $"Erro ao definir proteção temporária da seção {i} ({sectionName}): {error}";
                        return false;
                    }

                    uint remainingSize = virtualSize;
                    IntPtr currentAddress = sectionAddress;

                    while (remainingSize > 0)
                    {
                        uint writeSize = Math.Min(remainingSize, 0x1000);
                        if (!WriteProcessMemory(processHandle, currentAddress, zeroBuffer, writeSize, out bytesWritten))
                        {
                            uint error = GetLastError();
                            LastError = $"Erro ao zerar memória da seção {i} ({sectionName}) em 0x{currentAddress.ToInt64():X} - " +
                                      $"Tamanho: 0x{writeSize:X}, Erro: {error}";
                            return false;
                        }

                        remainingSize -= writeSize;
                        currentAddress = IntPtr.Add(currentAddress, (int)writeSize);
                    }

                    if (section.SizeOfRawData > 0 && section.PointerToRawData > 0)
                    {
                        uint rawDataSize = section.SizeOfRawData;
                        if (section.PointerToRawData + rawDataSize > dllBytes.Length)
                        {
                            rawDataSize = (uint)Math.Max(0, dllBytes.Length - section.PointerToRawData);
                            LastError = $"Ajustando tamanho da seção {i} ({sectionName}) de 0x{section.SizeOfRawData:X} para 0x{rawDataSize:X}";
                        }

                        if (rawDataSize > 0)
                        {
                            try
                            {
                                byte[] sectionData = new byte[rawDataSize];
                                Array.Copy(dllBytes, section.PointerToRawData, sectionData, 0, rawDataSize);

                                if (!WriteProcessMemory(processHandle, sectionAddress, sectionData, rawDataSize, out bytesWritten))
                                {
                                    LastError = $"Erro ao escrever seção {i} ({sectionName}) (Erro: {GetLastError()})";
                                    return false;
                                }
                            }
                            catch (Exception ex)
                            {
                                LastError = $"Erro ao copiar dados da seção {i} ({sectionName}): {ex.Message}";
                                return false;
                            }
                        }
                    }

                    uint protection = GetSectionProtection(section.Characteristics);

                    if (protection == 0x01 || protection == 0)
                    {
                        protection = 0x02;
                    }

                    if (!VirtualProtectEx(processHandle, sectionAddress,
                        new UIntPtr(virtualSize), protection, out oldProtect))
                    {
                        uint error = GetLastError();
                        LastError = $"Erro ao definir proteção da seção {i} ({sectionName}) - " +
                                  $"Endereço: 0x{sectionAddress.ToInt64():X}, " +
                                  $"Tamanho: 0x{virtualSize:X}, " +
                                  $"Proteção: 0x{protection:X}, " +
                                  $"Erro: {error}";
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                LastError = $"Erro ao mapear seções: {ex.Message}";
                return false;
            }
        }

        private bool ExecuteDllMain(IntPtr processHandle, IntPtr entryPoint)
        {
            try
            {
                const uint THREAD_CREATION_FLAGS = 0x00000004;
                const uint DLL_PROCESS_ATTACH = 1;

                LastError = $"Verificando compatibilidade antes de executar DllMain em 0x{entryPoint.ToInt64():X}";

                bool targetIsWow64 = false;
                if (!IsWow64Process(processHandle, out targetIsWow64))
                {
                    uint error = GetLastError();
                    LastError = $"Erro ao verificar arquitetura do processo alvo: {error}";
                    return false;
                }

                bool injectorIsWow64 = false;
                if (!IsWow64Process(Process.GetCurrentProcess().Handle, out injectorIsWow64))
                {
                    uint error = GetLastError();
                    LastError = $"Erro ao verificar arquitetura do injetor: {error}";
                    return false;
                }

                if (targetIsWow64 != injectorIsWow64)
                {
                    LastError = $"Incompatibilidade de arquitetura detectada - Processo alvo: {(targetIsWow64 ? "32-bit" : "64-bit")}, Injetor: {(injectorIsWow64 ? "32-bit" : "64-bit")}";
                    return false;
                }

                uint oldProtect;
                if (!VirtualProtectEx(processHandle, entryPoint, new UIntPtr(4), PAGE_EXECUTE_READWRITE, out oldProtect))
                {
                    uint error = GetLastError();
                    LastError = $"Erro ao verificar endereço do DllMain: {error} - Endereço: 0x{entryPoint.ToInt64():X}";
                    return false;
                }

                byte[] entryPointBytes = new byte[4];
                UIntPtr bytesRead;
                if (!ReadProcessMemory(processHandle, entryPoint, entryPointBytes, 4, out bytesRead))
                {
                    uint error = GetLastError();
                    LastError = $"Erro ao ler endereço do DllMain: {error} - Possível endereço inválido";
                    return false;
                }

                if (BitConverter.ToInt32(entryPointBytes, 0) == 0 ||
                    entryPointBytes.All(b => b == 0xCC))
                {
                    LastError = "DllMain parece estar corrompido ou não inicializado corretamente";
                    return false;
                }

                uint tempProtect;
                VirtualProtectEx(processHandle, entryPoint, new UIntPtr(4), oldProtect, out tempProtect);

                LastError = $"Criando thread remota em 0x{entryPoint.ToInt64():X}";

                IntPtr threadHandle = CreateRemoteThread(processHandle, IntPtr.Zero, 0,
                    entryPoint, new IntPtr(DLL_PROCESS_ATTACH), THREAD_CREATION_FLAGS, IntPtr.Zero);

                if (threadHandle == IntPtr.Zero)
                {
                    uint error = GetLastError();
                    LastError = $"Erro ao criar thread remota: {error}";
                    return false;
                }

                try
                {
                    LastError = "Iniciando execução da thread";

                    if (ResumeThread(threadHandle) == uint.MaxValue)
                    {
                        uint error = GetLastError();
                        LastError = $"Erro ao retomar thread: {error}";
                        return false;
                    }

                    LastError = "Aguardando execução do DllMain";
                    uint waitResult = WaitForSingleObject(threadHandle, 30000);

                    if (waitResult == 0x00000102L)
                    {
                        LastError = "Timeout ao aguardar execução do DllMain - Tentando obter status da thread";

                        uint exitCode = 0;
                        if (GetExitCodeThread(threadHandle, out exitCode))
                        {
                            if (exitCode == 259)
                            {
                                LastError = "Thread ainda está em execução - Tentando terminar";
                                TerminateThread(threadHandle, 0);
                            }
                            else if (exitCode == 0x3)
                            {
                                LastError = "DllMain retornou ERROR_PATH_NOT_FOUND - Possível falta de dependências";
                            }
                            else if ((exitCode & 0xFFFF0000) == 0xFFFF0000)
                            {
                                string errorDesc = GetCustomErrorDescription(exitCode);
                                LastError = $"DllMain retornou erro personalizado: 0x{exitCode:X} - {errorDesc}";
                            }
                            else
                            {
                                LastError = $"Thread terminou com código: 0x{exitCode:X}";
                            }
                        }

                        return false;
                    }
                    else if (waitResult != 0)
                    {
                        uint error = GetLastError();
                        LastError = $"Erro ao aguardar thread: {error}";
                        return false;
                    }

                    uint threadExitCode = 0;
                    if (GetExitCodeThread(threadHandle, out threadExitCode))
                    {
                        if (threadExitCode == 0x3)
                        {
                            LastError = "DllMain retornou ERROR_PATH_NOT_FOUND - Verifique se todas as dependências da DLL estão presentes no sistema";
                            return false;
                        }
                        else if ((threadExitCode & 0xFFFF0000) == 0xFFFF0000)
                        {
                            string errorDesc = GetCustomErrorDescription(threadExitCode);
                            LastError = $"DllMain retornou erro personalizado: 0x{threadExitCode:X} - {errorDesc}";
                            return false;
                        }
                        else if (threadExitCode != 0)
                        {
                            LastError = $"DllMain retornou erro: 0x{threadExitCode:X}";
                            return false;
                        }
                    }

                    LastError = "DllMain executado com sucesso";
                    return true;
                }
                finally
                {
                    CloseHandle(threadHandle);
                }
            }
            catch (Exception ex)
            {
                LastError = $"Erro ao executar DllMain: {ex.Message}";
                return false;
            }
        }

        private uint GetSectionProtection(uint characteristics)
        {
            const uint IMAGE_SCN_MEM_EXECUTE = 0x20000000;
            const uint IMAGE_SCN_MEM_READ = 0x40000000;
            const uint IMAGE_SCN_MEM_WRITE = 0x80000000;

            uint protect = 0;

            if ((characteristics & IMAGE_SCN_MEM_EXECUTE) != 0)
            {
                if ((characteristics & IMAGE_SCN_MEM_READ) != 0)
                {
                    if ((characteristics & IMAGE_SCN_MEM_WRITE) != 0)
                        protect = PAGE_EXECUTE_READWRITE;
                    else
                        protect = 0x20;
                }
                else
                    protect = 0x10;
            }
            else
            {
                if ((characteristics & IMAGE_SCN_MEM_READ) != 0)
                {
                    if ((characteristics & IMAGE_SCN_MEM_WRITE) != 0)
                        protect = 0x04;
                    else
                        protect = 0x02;
                }
                else
                    protect = 0x01;
            }

            return protect;
        }

        private static T ByteArrayToStructure<T>(byte[] bytes, int offset = 0) where T : struct
        {
            int size = Marshal.SizeOf<T>();
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(bytes, offset, ptr, size);
                return Marshal.PtrToStructure<T>(ptr);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        public static int GetProcessIdByName(string processName)
        {
            try
            {
                var process = System.Diagnostics.Process.GetProcessesByName(
                    System.IO.Path.GetFileNameWithoutExtension(processName)).FirstOrDefault();
                return process?.Id ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        private string GetCustomErrorDescription(uint errorCode)
        {
            switch (errorCode)
            {
                case 0xFFFF7000:
                    return "Erro de inicialização da DLL";

                case 0xFFFF7001:
                    return "Erro de compatibilidade de versão";

                case 0xFFFF7002:
                    return "Erro de dependência ou recurso necessário não encontrado";

                case 0xFFFF7003:
                    return "Erro de permissão ou acesso negado";

                case 0xFFFF7004:
                    return "Erro de incompatibilidade de arquitetura";

                default:
                    return "Possível incompatibilidade ou erro interno da DLL";
            }
        }

        private IntPtr GetKernelBase()
        {
            try
            {
                LastError = "Procurando base do kernel32.dll";

                IntPtr hModule = GetModuleHandle("kernel32.dll");
                if (hModule != IntPtr.Zero)
                {
                    LastError = $"Base do kernel32.dll encontrada em 0x{hModule.ToInt64():X}";
                    return hModule;
                }

                foreach (ProcessModule module in Process.GetCurrentProcess().Modules)
                {
                    if (module.ModuleName.ToLower() == "kernel32.dll")
                    {
                        LastError = $"Base do kernel32.dll encontrada em módulos: 0x{module.BaseAddress.ToInt64():X}";
                        return module.BaseAddress;
                    }
                }

                LastError = "Não foi possível localizar kernel32.dll";
                return IntPtr.Zero;
            }
            catch (Exception ex)
            {
                LastError = $"Erro ao procurar kernel32.dll: {ex.Message}";
                return IntPtr.Zero;
            }
        }

        private IntPtr GetProcAddressWithHash(IntPtr moduleBase, uint functionHash)
        {
            try
            {
                LastError = $"Procurando função com hash 0x{functionHash:X}";

                if (functionHash == 0x726774C)
                {
                    IntPtr procAddr = GetProcAddress(moduleBase, "LoadLibraryA");
                    if (procAddr != IntPtr.Zero)
                    {
                        LastError = $"LoadLibraryA encontrada em 0x{procAddr.ToInt64():X}";
                        return procAddr;
                    }
                }

                IMAGE_DOS_HEADER dosHeader = Marshal.PtrToStructure<IMAGE_DOS_HEADER>(moduleBase);
                if (dosHeader.e_magic != 0x5A4D)
                {
                    LastError = "Cabeçalho DOS inválido";
                    return IntPtr.Zero;
                }

                IntPtr ntHeaderPtr = IntPtr.Add(moduleBase, dosHeader.e_lfanew);
                IMAGE_NT_HEADERS ntHeaders = Marshal.PtrToStructure<IMAGE_NT_HEADERS>(ntHeaderPtr);

                if (ntHeaders.Signature != 0x4550)
                {
                    LastError = "Cabeçalho PE inválido";
                    return IntPtr.Zero;
                }

                int exportDirRva = ntHeaders.OptionalHeader32.ExportTable.VirtualAddress;
                if (exportDirRva == 0)
                {
                    LastError = "Diretório de exportação não encontrado";
                    return IntPtr.Zero;
                }

                IntPtr exportDirPtr = IntPtr.Add(moduleBase, exportDirRva);
                IMAGE_EXPORT_DIRECTORY exportDir = Marshal.PtrToStructure<IMAGE_EXPORT_DIRECTORY>(exportDirPtr);

                IntPtr namesPtr = IntPtr.Add(moduleBase, (int)exportDir.AddressOfNames);
                IntPtr ordinalsPtr = IntPtr.Add(moduleBase, (int)exportDir.AddressOfNameOrdinals);
                IntPtr functionsPtr = IntPtr.Add(moduleBase, (int)exportDir.AddressOfFunctions);

                for (int i = 0; i < exportDir.NumberOfNames; i++)
                {
                    int nameRva = Marshal.ReadInt32(IntPtr.Add(namesPtr, i * 4));
                    string functionName = Marshal.PtrToStringAnsi(IntPtr.Add(moduleBase, nameRva));

                    if (HashString(functionName) == functionHash)
                    {
                        short ordinal = Marshal.ReadInt16(IntPtr.Add(ordinalsPtr, i * 2));
                        int functionRva = Marshal.ReadInt32(IntPtr.Add(functionsPtr, ordinal * 4));
                        IntPtr functionAddr = IntPtr.Add(moduleBase, functionRva);

                        LastError = $"Função encontrada: {functionName} em 0x{functionAddr.ToInt64():X}";
                        return functionAddr;
                    }
                }

                LastError = "Função não encontrada";
                return IntPtr.Zero;
            }
            catch (Exception ex)
            {
                LastError = $"Erro ao procurar função: {ex.Message}";
                return IntPtr.Zero;
            }
        }

        private uint HashString(string input)
        {
            uint hash = 0;
            foreach (char c in input)
            {
                hash = ((hash << 5) + hash) + c;
            }
            return hash;
        }
    }
}