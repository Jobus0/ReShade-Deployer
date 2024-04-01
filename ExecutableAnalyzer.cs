using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ReShade.Setup.Utilities
{
	public unsafe class ExecutableParser
	{
		public enum ImageDirectory : UInt16
		{
			EntryExport = 0,
			EntryImport = 1,
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct LoadedImage
		{
			public IntPtr ModuleName;
			public IntPtr hFile;
			public IntPtr MappedAddress;
			public IntPtr FileHeader;
			public IntPtr LastRvaSection;
			public UInt32 NumberOfSections;
			public IntPtr Sections;
			public UInt32 Characteristics;
			public UInt16 fSystemImage;
			public UInt16 fDOSImage;
			public UInt16 fReadOnly;
			public UInt16 Version;
			public IntPtr Flink;
			public IntPtr BLink;
			public UInt32 SizeOfImage;
		}

		[StructLayout(LayoutKind.Explicit)]
		private struct ImageImportDescriptor
		{
			[FieldOffset(0)]
			public UInt32 Characteristics;
			[FieldOffset(0)]
			public UInt32 OriginalFirstThunk;
			[FieldOffset(4)]
			public UInt32 TimeDateStamp;
			[FieldOffset(8)]
			public UInt32 ForwarderChain;
			[FieldOffset(12)]
			public UInt32 Name;
			[FieldOffset(16)]
			public UInt32 FirstThunk;
		}

		[DllImport("imagehlp.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool MapAndLoad([In, MarshalAs(UnmanagedType.LPStr)] string imageName, [In, MarshalAs(UnmanagedType.LPStr)] string? dllPath, [Out] out LoadedImage loadedImage, [In, MarshalAs(UnmanagedType.Bool)] bool dotDll, [In, MarshalAs(UnmanagedType.Bool)] bool readOnly);
		
		[DllImport("imagehlp.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool UnMapAndLoad([In] ref LoadedImage loadedImage);

		[DllImport("dbghelp.dll", SetLastError = true)]
		private static extern IntPtr ImageRvaToVa([In] IntPtr pNtHeaders, [In] IntPtr pBase, [In] uint rva, [In] IntPtr pLastRvaSection);
		
		[DllImport("dbghelp.dll", SetLastError = true)]
		private static extern IntPtr ImageDirectoryEntryToData([In] IntPtr pBase, [In, MarshalAs(UnmanagedType.U1)] bool mappedAsImage, [In] ImageDirectory directoryEntry, [Out] out uint size);

		public IEnumerable<string> Modules { get; }
		
		// Adapted from http://stackoverflow.com/a/4696857/2055880
		public ExecutableParser(string path)
		{
			var modules = new List<string>();

			if (MapAndLoad(path, null, out LoadedImage image, false, true))
			{
				var imports = (ImageImportDescriptor*)ImageDirectoryEntryToData(image.MappedAddress, false, ImageDirectory.EntryImport, out _);

				if (imports != null)
				{
					while (imports->OriginalFirstThunk != 0)
					{
						string? module = Marshal.PtrToStringAnsi(ImageRvaToVa(image.FileHeader, image.MappedAddress, imports->Name, IntPtr.Zero));
						if (!string.IsNullOrEmpty(module))
						{
							modules.Add(module);
						}

						++imports;
					}
				}

				UnMapAndLoad(ref image);
			}

			Modules = modules;
		}
		
		public bool ModulesContain(string moduleName)
		{
			foreach (var module in Modules)
			{
				if (module.StartsWith(moduleName, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}
		
		public bool ModulesContain(params string[] moduleNames)
		{
			foreach (var module in Modules)
			{
				foreach (var moduleName in moduleNames)
				{
					if (module.StartsWith(moduleName, StringComparison.OrdinalIgnoreCase))
					{
						return true;
					}
				}
			}

			return false;
		}
	}
}
