﻿using ExplorerEx.View;
using ExplorerEx.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using ExplorerEx.Shell32;
using System.Threading;
using FileAttribute = ExplorerEx.Shell32.FileAttribute;

namespace ExplorerEx.Utils;
internal static class FileUtils {
	private static readonly Dictionary<string, string> CachedDescriptions = new();

	private static readonly HashSet<string> ProhibitedFileNames = new() {
		"con",
		"prn",
		"aux",
		"nul",
		"com1",
		"com2",
		"com3",
		"com4",
		"com5",
		"com6",
		"com7",
		"com8",
		"com9",
		"lpt1",
		"lpt2",
		"lpt3",
		"lpt4",
		"lpt5",
		"lpt6",
		"lpt7",
		"lpt8",
		"lpt9"
	};

	/// <summary>
	/// 是否为可执行文件
	/// </summary>
	/// <param name="filePath"></param>
	/// <returns></returns>
	public static bool IsExecutable(string filePath) {
		return filePath.Length > 7 && filePath[^4..] is ".exe" or ".com" or ".cmd" or ".bat";
	}

	/// <summary>
	/// 判断是否是非法的文件名，传入的是文件名
	/// </summary>
	/// <param name="fileName"></param>
	/// <returns></returns>
	public static bool IsProhibitedFileName(string fileName) {
		if (fileName == null) {
			return true;
		}
		fileName = fileName.Trim();
		if (fileName == string.Empty) {
			return true;
		}
		if (ProhibitedFileNames.Contains(fileName.ToLower())) {
			return true;
		}
		return fileName.Any(c => c is '\\' or '/' or ':' or '*' or '?' or '"' or '<' or '>' or '|');
	}

	private static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

	/// <summary>
	/// 将bytes单位的数字格式化成更易读的形式
	/// </summary>
	/// <param name="sizeInBytes"></param>
	/// <returns></returns>
	public static string FormatByteSize(long sizeInBytes) {
		switch (sizeInBytes) {
		case < 0:
			return string.Empty;
		case 0:
			return "0 byte";
		case 1:
			return "1 byte";
		default:
			var mag = (int)Math.Log(sizeInBytes, 1024);

			// 1L << (mag * 10) == 2 ^ (10 * mag) 
			// [i.e. the number of bytes in the unit corresponding to mag]
			var adjustedSize = (double)sizeInBytes / (1L << (mag * 10));

			// make adjustment when the value is large enough that
			// it would round up to 1000 or more
			if (Math.Round(adjustedSize, 1) >= 1000) {
				mag += 1;
				adjustedSize /= 1024;
			}

			return $"{adjustedSize:n1} {SizeSuffixes[mag]}";
		}
	}

	/// <summary>
	/// 寻找文件的位置，例如传入cmd就会返回C:\Windows\System32\cmd.exe
	/// </summary>
	/// <param name="fileName"></param>
	/// <returns></returns>
	public static string FindFileLocation(string fileName) {
		if (fileName == null) {
			return null;
		}
		if (File.Exists(fileName)) {
			return fileName;
		}
		var where = Process.Start(new ProcessStartInfo("where", fileName) {
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			UseShellExecute = false
		});
		if (where != null) {
			while (true) {
				var location = where.StandardOutput.ReadLine();
				if (location == null) {
					return null;
				}
				if (File.Exists(location)) {
					return location;
				}
			}
		}
		return null;
	}

	/// <summary>
	/// 根据拓展名获取文件类型的描述
	/// </summary>
	/// <param name="extension"></param>
	/// <returns></returns>
	public static string GetFileTypeDescription(string extension) {
		if (string.IsNullOrEmpty(extension)) {
			return null;
		}
		extension = extension.ToLower();
		lock (CachedDescriptions) {
			if (CachedDescriptions.TryGetValue(extension, out var desc)) {
				return desc;
			}
		}

		var shFileInfo = new ShFileInfo();
		Monitor.Enter(Shell32Interop.ShellLock);
		var res = Shell32Interop.SHGetFileInfo(extension, FileAttribute.Normal, ref shFileInfo, Marshal.SizeOf(shFileInfo), SHGFI.UseFileAttributes | SHGFI.TypeName);
		Monitor.Exit(Shell32Interop.ShellLock);
		if (res == 0 || shFileInfo.szTypeName == null) {
			Trace.WriteLine($"无法获取 {extension} 的描述，Res: {res}");
			return null;
		}

		lock (CachedDescriptions) {
			CachedDescriptions[extension] = shFileInfo.szTypeName;
		}
		return shFileInfo.szTypeName;
	}

	/// <summary>
	/// 枚举<see cref="destPath"/>，获取一个不重名的文件名
	/// </summary>
	/// <para>例如fileName为2.txt，文件夹里有2.txt, 2 (1).txt，那么就返回2 (2).txt</para>
	/// <param name="destPath"></param>
	/// <param name="fileName"></param>
	/// <returns></returns>
	public static string GetNewFileName(string destPath, string fileName) {
		var newFileName = fileName;
		var extension = Path.GetExtension(fileName);
		var sameNameCount = 0;
		// 这里应该不需要使用哈希表，毕竟数量不多，枚举不需要消耗太多时间，节省内存
		var destFileList = Directory.EnumerateFiles(destPath, "*" + extension).Select(Path.GetFileName).ToArray();
		while (destFileList.Contains(newFileName)) {
			newFileName = $"{fileName} ({++sameNameCount}){extension}";
		}
		return newFileName;
	}

	/// <summary>
	/// 获取.lnk文件的目标位置，如果错误返回null
	/// </summary>
	/// <param name="shortcutPath"></param>
	/// <returns></returns>
	public static string GetShortcutTargetPath(string shortcutPath) {
		try {
			return Shell32Interop.GetLnkTargetPath(shortcutPath);
		} catch {
			return null;
		}
	}

	/// <summary>
	/// 获取一个文件的父目录的路径，如果是.lnk文件就获取目标位置
	/// </summary>
	/// <param name="filePath"></param>
	/// <returns></returns>
	public static string GetFileLocation(string filePath) {
		if (filePath.Length <= 3) {
			return null;
		}
		if (filePath[^4..] == ".lnk") {
			return Path.GetDirectoryName(GetShortcutTargetPath(filePath));
		}
		return Path.GetDirectoryName(filePath);
	}

	/// <summary>
	/// Shell文件操作
	/// </summary>
	/// <param name="type"></param>
	/// <param name="sourceFiles"></param>
	/// <param name="destinationFiles"></param>
	/// <exception cref="ArgumentException"></exception>
	/// <exception cref="IOException"></exception>
	public static void FileOperation(FileOpType type, IList<string> sourceFiles, IList<string> destinationFiles = null) {
		if (sourceFiles is not { Count: > 0 }) {
			return;
		}
		if (type != FileOpType.Delete && (destinationFiles == null || sourceFiles.Count != destinationFiles.Count)) {
			throw new ArgumentException("原文件与目标文件个数不匹配");
		}
		var fFlags = FileOpFlags.NoConfirmMkdir | FileOpFlags.AllowUndo;
		if (sourceFiles.Count > 1) {
			fFlags |= FileOpFlags.MultiDestFiles;
		}
		switch (type) {
		case FileOpType.Delete:
			fFlags |= FileOpFlags.NoConformation;
			break;
		case FileOpType.Copy:
			fFlags |= FileOpFlags.RenameOnCollision;
			break;
		}
		var fo = new ShFileOpStruct {
			fileOpType = type,
			pFrom = ParseFileList(sourceFiles),
			fFlags = fFlags
		};
		if (type != FileOpType.Delete) {
			fo.pTo = ParseFileList(destinationFiles);
		}
		var result = Shell32Interop.SHFileOperation(fo);
		if (result is not 0 and not 1223) {  // 1223: 用户取消了操作
			throw new IOException(GetErrorPrompting(result));
		}
	}

	/// <summary>
	/// Shell文件操作
	/// </summary>
	/// <param name="type"></param>
	/// <param name="sourceFile"></param>
	/// <param name="destinationFile"></param>
	/// <exception cref="ArgumentNullException"></exception>
	/// <exception cref="ArgumentException"></exception>
	/// <exception cref="IOException"></exception>
	public static void FileOperation(FileOpType type, string sourceFile, string destinationFile = null) {
		if (sourceFile == null) {
			throw new ArgumentNullException(nameof(sourceFile));
		}
		if (type != FileOpType.Delete && destinationFile == null) {
			throw new ArgumentNullException(nameof(destinationFile));
		}
		var fFlags = FileOpFlags.NoConfirmMkdir | FileOpFlags.AllowUndo;
		switch (type) {
		case FileOpType.Delete:
			fFlags |= FileOpFlags.NoConformation;
			break;
		case FileOpType.Copy:
			fFlags |= FileOpFlags.RenameOnCollision;
			break;
		}
		var fo = new ShFileOpStruct {
			fileOpType = type,
			pFrom = sourceFile + '\0',
			fFlags = fFlags
		};
		if (type != FileOpType.Delete) {
			fo.pTo = destinationFile + '\0';
		}
		var result = Shell32Interop.SHFileOperation(fo);
		if (result != 0) {
			throw new IOException(GetErrorPrompting(result));
		}
	}

	/// <summary>
	/// 将<see cref="sourceFiles"/>拷贝到目标文件夹
	/// </summary>
	/// <param name="sourceFiles"></param>
	/// <param name="destFolder"></param>
	public static void CopyFiles(IList<string> sourceFiles, string destFolder) {

	}

	public static void HandleDrop(DataObjectContent content, string destPath, DragDropEffects type) {
		if (destPath == null) {
			return;
		}
		Debug.Assert(type is DragDropEffects.Copy or DragDropEffects.Move or DragDropEffects.Link);
		if (destPath.Length > 4 && destPath[^4..] is ".exe" or ".lnk") {  // 拖文件运行
			if (File.Exists(destPath) && content.Type == DataObjectType.FileDrop) {
				try {
					Process.Start(new ProcessStartInfo {
						FileName = destPath,
						Arguments = string.Join(' ', (string[])content.Data),
						UseShellExecute = true
					});
				} catch (Exception ex) {
					Logger.Exception(ex);
				}
			}
		} else {
			if (!Directory.Exists(destPath)) {
				destPath = Path.GetDirectoryName(destPath);
			}
			if (Directory.Exists(destPath)) {
				switch (content.Type) {
				case DataObjectType.FileDrop:
					var filePaths = (string[])content.Data;
					if (filePaths is { Length: > 0 }) {
						var destPaths = filePaths.Select(p => Path.Combine(destPath, Path.GetFileName(p))).ToList();
						try {
							if (type == DragDropEffects.Link) {
								for (var i = 0; i < filePaths.Length; i++) {
									var path = destPaths[i];
									if (path.Length == 3) {  // 处理驱动器号这种特殊情况
										path = path + filePaths[i][0] + ".lnk";
									} else {
										path = Path.ChangeExtension(path, ".lnk");
									}
									Shell32Interop.CreateLnk(filePaths[i], path);
								}
							} else {
								FileOperation(type == DragDropEffects.Move ? FileOpType.Move : FileOpType.Copy, filePaths, destPaths);
							}
						} catch (Exception ex) {
							Logger.Exception(ex);
						}
					}
					break;
				case DataObjectType.Bitmap:
					break;
				case DataObjectType.Html:
					new SaveDataObjectWindow(destPath, content.Data.ToString()).Show();
					break;
				case DataObjectType.Text:
					new SaveDataObjectWindow(destPath, content.Data.ToString()).Show();
					break;
				case DataObjectType.UnicodeText:
					new SaveDataObjectWindow(destPath, content.Data.ToString()).Show();
					break;
				}
			}
		}
	}

	private static string ParseFileList(IEnumerable<string> files) {
		var sb = new StringBuilder();
		foreach (var file in files) {
			sb.Append(Path.GetFullPath(file)).Append('\0');
		}
		return sb.Append('\0').ToString();
	}

	/// <summary>
	/// ms-help://MS.MSDNQTR.v90.chs/shellcc/platform/shell/reference/functions/shfileoperation.htm
	/// </summary>
	/// <param name="code"></param>
	/// <returns></returns>
	private static string GetErrorPrompting(int code) {
		return code switch {
			0 => "Success",
			0x74 => "The source is a root directory, which cannot be moved or renamed.",
			0x76 => "Security settings denied access to the source.",
			0x7C => "The path in the source or destination or both was invalid.",
			0x10000 => "An unspecified error occurred on the destination.",
			0x402 => "An unknown error occurred. This is typically due to an invalid path in the source or destination. This error does not occur on Windows Vista and later.",
			_ => Marshal.GetExceptionForHR(code)?.Message ?? $"Unknown error: {code}"
		};
	}

	/// <summary>
	/// 判断一个文件是否为文本文件
	/// </summary>
	/// <para> From: https://social.msdn.microsoft.com/Forums/vstudio/en-US/c177719a-4671-4435-aa4f-7a92852be6cc/how-can-i-determine-if-a-file-is-binary-or-text-in-c </para>
	/// <param name="encoding"></param>
	/// <param name="filePath"></param>
	/// <param name="windowSize"></param>
	/// <returns></returns>
	public static bool IsTextFile(out Encoding encoding, string filePath, int windowSize = 10240) {
		using var fileStream = File.OpenRead(filePath);
		var rawData = new byte[windowSize];
		var text = new char[windowSize];
		var isText = true;

		// Read raw bytes
		var rawLength = fileStream.Read(rawData, 0, rawData.Length);
		fileStream.Seek(0, SeekOrigin.Begin);

		// Detect encoding correctly (from Rick Strahl's blog)
		// http://www.west-wind.com/weblog/posts/2007/Nov/28/Detecting-Text-Encoding-for-StreamReader
		encoding = rawData[0] switch {
			0x00 when rawData[1] == 0x00 && rawData[2] == 0xfe && rawData[3] == 0xff => Encoding.UTF32,
			// 0x2b when rawData[1] == 0x2f && rawData[2] == 0x76 => Encoding.UTF7,
			0xef when rawData[1] == 0xbb && rawData[2] == 0xbf => Encoding.UTF8,
			0xfe when rawData[1] == 0xff => Encoding.Unicode,
			_ => Encoding.Default
		};

		// Read text and detect the encoding
		using (var streamReader = new StreamReader(fileStream)) {
			streamReader.Read(text, 0, text.Length);
		}

		using var memoryStream = new MemoryStream();
		using var streamWriter = new StreamWriter(memoryStream, encoding);
		// Write the text to a buffer
		streamWriter.Write(text);
		streamWriter.Flush();

		// Get the buffer from the memory stream for comparision
		var memoryBuffer = memoryStream.GetBuffer();

		// Compare only bytes read
		for (var i = 0; i < rawLength && isText; i++) {
			isText = rawData[i] == memoryBuffer[i];
		}

		return isText;
	}
}