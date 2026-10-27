using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace RelayDesk.Services;

// Windows Data Protection API, bound to the current Windows user account.
// Avoids any NuGet dependencies and keeps keys out of the settings JSON.
public static class SecretStore
{
    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob
    {
        public int Length;
        public IntPtr Data;
    }

    private const int CryptProtectUiForbidden = 0x1;

    [DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptProtectData(
        ref DataBlob input, string? description, IntPtr optionalEntropy,
        IntPtr reserved, IntPtr prompt, int flags, out DataBlob output);

    [DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptUnprotectData(
        ref DataBlob input, IntPtr description, IntPtr optionalEntropy,
        IntPtr reserved, IntPtr prompt, int flags, out DataBlob output);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LocalFree(IntPtr value);

    public static string Protect(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return "";
        return Convert.ToBase64String(Transform(Encoding.UTF8.GetBytes(plainText), protect: true));
    }

    public static string Unprotect(string encrypted)
    {
        if (string.IsNullOrEmpty(encrypted)) return "";
        return Encoding.UTF8.GetString(Transform(Convert.FromBase64String(encrypted), protect: false));
    }

    private static byte[] Transform(byte[] inputBytes, bool protect)
    {
        var input = new DataBlob { Length = inputBytes.Length, Data = Marshal.AllocHGlobal(inputBytes.Length) };
        try
        {
            Marshal.Copy(inputBytes, 0, input.Data, inputBytes.Length);
            bool success;
            DataBlob output;
            if (protect)
                success = CryptProtectData(ref input, "RelayDesk stream key", IntPtr.Zero,
                    IntPtr.Zero, IntPtr.Zero, CryptProtectUiForbidden, out output);
            else
                success = CryptUnprotectData(ref input, IntPtr.Zero, IntPtr.Zero,
                    IntPtr.Zero, IntPtr.Zero, CryptProtectUiForbidden, out output);

            if (!success) throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows could not protect or read the stream key.");
            try
            {
                var result = new byte[output.Length];
                Marshal.Copy(output.Data, result, 0, result.Length);
                return result;
            }
            finally
            {
                if (output.Data != IntPtr.Zero) LocalFree(output.Data);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(input.Data);
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(inputBytes);
        }
    }
}
