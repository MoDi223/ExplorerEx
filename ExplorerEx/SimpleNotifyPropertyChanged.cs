﻿using System.ComponentModel;
using System.Runtime.CompilerServices;
using ExplorerEx.Annotations;

namespace ExplorerEx;

public class SimpleNotifyPropertyChanged : INotifyPropertyChanged {
	public event PropertyChangedEventHandler PropertyChanged;

	[NotifyPropertyChangedInvocator]
	public void UpdateUI([CallerMemberName] string propertyName = null) {
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}