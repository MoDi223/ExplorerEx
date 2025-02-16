﻿using System.IO;

namespace ExplorerEx.Utils; 

public static class DriveUtils {
	/// <summary>
	/// 获取DriveInfo的类型描述
	/// </summary>
	/// <param name="driveInfo"></param>
	/// <returns></returns>
	public static string GetTypeDescription(DriveInfo driveInfo) {
		return driveInfo.DriveType switch {
			DriveType.Removable => "Removable_disk".L(),
			DriveType.CDRom => "CD_drive".L(),
			DriveType.Fixed => "Local_disk".L(),
			_ => "Other_type_disk".L()
		};
	}

	/// <summary>
	/// 获取DriveInfo的友好名称，如 系统 (C:) 本地磁盘 (D:)
	/// </summary>
	/// <param name="driveInfo"></param>
	/// <returns></returns>
	public static string GetFriendlyName(DriveInfo driveInfo) {
		if (driveInfo.IsReady) {
			return $"{(string.IsNullOrWhiteSpace(driveInfo.VolumeLabel) ? GetTypeDescription(driveInfo) : driveInfo.VolumeLabel)} ({driveInfo.Name[..1]})";
		}
		return $"{GetFriendlyName(driveInfo)} ({driveInfo.Name[..1]})";
	}
}