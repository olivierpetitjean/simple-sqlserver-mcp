using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace SimpleSqlServerMcp.WindowsInstaller;

public sealed class WindowsCredentialWriter : IWindowsCredentialWriter
{
    private const int CredTypeGeneric = 1;
    private const int CredPersistLocalMachine = 2;

    public void WriteGenericCredential(string targetName, string secret, string? username)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetName);
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);

        byte[] secretBytes = Encoding.Unicode.GetBytes(secret);
        IntPtr secretBuffer = Marshal.AllocCoTaskMem(secretBytes.Length);

        try
        {
            Marshal.Copy(secretBytes, 0, secretBuffer, secretBytes.Length);

            NativeCredential credential = new()
            {
                Type = CredTypeGeneric,
                TargetName = targetName,
                CredentialBlobSize = secretBytes.Length,
                CredentialBlob = secretBuffer,
                Persist = CredPersistLocalMachine,
                UserName = username,
            };

            if (!CredWrite(ref credential, 0))
            {
                throw new InvalidOperationException(
                    $"Writing the Windows Credential Manager entry `{targetName}` failed with Win32 error {Marshal.GetLastWin32Error()}.");
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secretBytes);
            Marshal.FreeCoTaskMem(secretBuffer);
        }
    }

    [DllImport("Advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredWrite([In] ref NativeCredential credential, [In] uint flags);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeCredential
    {
        public int Flags;
        public int Type;
        public string TargetName;
        public string? Comment;
        public long LastWritten;
        public int CredentialBlobSize;
        public IntPtr CredentialBlob;
        public int Persist;
        public int AttributeCount;
        public IntPtr Attributes;
        public string? TargetAlias;
        public string? UserName;
    }
}
