﻿////////////////////////////////////////////////////////////////////////
//
// This file is part of gmic-sharp, a .NET wrapper for G'MIC.
//
// Copyright (c) 2020 Nicholas Hayes
//
// This file is licensed under the MIT License.
// See LICENSE.txt for complete licensing and attribution information.
//
////////////////////////////////////////////////////////////////////////

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace GmicSharp.Interop
{
    internal sealed class WindowsLibraryLoader : LibraryLoader
    {
        private const string DllFileExtension = ".dll";

        public WindowsLibraryLoader() : base(DllFileExtension)
        {
        }

        protected override LoadLibraryResult LoadLibrary(string path)
        {
            IntPtr handle = IntPtr.Zero;

            // Disable the error dialog that LoadLibrary shows if it cannot find a DLL dependency.
            using (new DisableLoadLibraryErrorDialog())
            {
                handle = NativeMethods.LoadLibraryW(path);
            }

            if (handle != IntPtr.Zero)
            {
                return new LoadLibraryResult(handle);
            }
            else
            {
                return new LoadLibraryResult(new Win32Exception(Marshal.GetLastWin32Error()));
            }
        }

        protected override IntPtr ResolveExportedSymbol(IntPtr libraryHandle, string name)
        {
            return NativeMethods.GetProcAddress(libraryHandle, name);
        }

        private static class NativeConstants
        {
            public const uint SEM_FAILCRITICALERRORS = 1;
        }

        private static class NativeMethods
        {
            [DllImport("kernel32.dll", EntryPoint = "LoadLibraryW")]
            public static extern IntPtr LoadLibraryW([In(), MarshalAs(UnmanagedType.LPWStr)] string lpLibFileName);

            [DllImport("kernel32.dll", EntryPoint = "GetProcAddress")]
            public static extern IntPtr GetProcAddress([In()] IntPtr hModule, [In(), MarshalAs(UnmanagedType.LPStr)] string lpProcName);

            [DllImport("kernel32.dll", EntryPoint = "SetErrorMode")]
            public static extern uint SetErrorMode([In()] uint uMode);

            [DllImport("kernel32.dll", EntryPoint = "SetThreadErrorMode")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetThreadErrorMode([In()] uint dwNewMode, [Out()] out uint lpOldMode);
        }

        private sealed class DisableLoadLibraryErrorDialog : IDisposable
        {
            private static readonly bool isWindows7OrLater = IsWindows7OrLater();

            private readonly uint oldMode;

            public DisableLoadLibraryErrorDialog()
            {
                oldMode = SetErrorMode(NativeConstants.SEM_FAILCRITICALERRORS);
            }

            private static bool IsWindows7OrLater()
            {
                OperatingSystem operatingSystem = Environment.OSVersion;

                return operatingSystem.Platform == PlatformID.Win32NT && operatingSystem.Version >= new Version(6, 1);
            }

            private static uint SetErrorMode(uint newMode)
            {
                uint oldMode;

                if (isWindows7OrLater)
                {
                    NativeMethods.SetThreadErrorMode(newMode, out oldMode);
                }
                else
                {
                    oldMode = NativeMethods.SetErrorMode(0);
                    NativeMethods.SetErrorMode(oldMode | newMode);
                }

                return oldMode;
            }

            public void Dispose()
            {
                SetErrorMode(oldMode);
            }
        }

    }
}
