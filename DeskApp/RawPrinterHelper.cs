using System;
using System.IO;
using System.Runtime.InteropServices;

namespace DeskApp
{
    internal static class RawPrinterHelper
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private class DOCINFOW
        {
            [MarshalAs(UnmanagedType.LPWStr)] public string pDocName;
            [MarshalAs(UnmanagedType.LPWStr)] public string pOutputFile;
            [MarshalAs(UnmanagedType.LPWStr)] public string pDataType;
        }

        [DllImport("winspool.Drv", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

        [DllImport("winspool.Drv", SetLastError = true)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] DOCINFOW pDocInfo);

        [DllImport("winspool.Drv", SetLastError = true)]
        private static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", SetLastError = true)]
        private static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", SetLastError = true)]
        private static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", SetLastError = true)]
        private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        public static bool SendBytesToPrinter(string printerName, IntPtr pBytes, int count)
        {
            if (string.IsNullOrWhiteSpace(printerName)) return false;
            if (pBytes == IntPtr.Zero || count <= 0) return false;

            if (!OpenPrinter(printerName, out var hPrinter, IntPtr.Zero))
                return false;

            try
            {
                var di = new DOCINFOW { pDocName = "Raw Document", pDataType = "RAW" };
                if (!StartDocPrinter(hPrinter, 1, di))
                    return false;

                if (!StartPagePrinter(hPrinter))
                {
                    EndDocPrinter(hPrinter);
                    return false;
                }

                var success = WritePrinter(hPrinter, pBytes, count, out var written);

                EndPagePrinter(hPrinter);
                EndDocPrinter(hPrinter);

                return success && written == count;
            }
            finally
            {
                ClosePrinter(hPrinter);
            }
        }

        public static bool SendFileToPrinter(string printerName, string fileName)
        {
            if (!File.Exists(fileName)) return false;
            var bytes = File.ReadAllBytes(fileName);
            var handle = Marshal.AllocCoTaskMem(bytes.Length);
            try
            {
                Marshal.Copy(bytes, 0, handle, bytes.Length);
                return SendBytesToPrinter(printerName, handle, bytes.Length);
            }
            finally
            {
                Marshal.FreeCoTaskMem(handle);
            }
        }
    }
}
