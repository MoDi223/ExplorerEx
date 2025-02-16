﻿using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media;
using ExplorerEx.Utils;
using System.Collections.Generic;

namespace ExplorerEx.View.Controls; 

public sealed partial class VideoPreviewPopup {
	public static VideoPreviewPopup Instance { get; } = new();

	private readonly Dictionary<Uri, TimeSpan> playHistory = new();
	private MediaTimeline timeline;
	private ClockController controller;

	static VideoPreviewPopup() { }

	private VideoPreviewPopup() {
		InitializeComponent();
	}

	private void Timeline_OnCurrentTimeInvalidated(object sender, EventArgs e) {
		if (controller == null) {
			return;
		}
		if (controller.Clock.NaturalDuration.HasTimeSpan && controller.Clock.CurrentTime.HasValue) {
			var currentTime = controller.Clock.CurrentTime.Value;
			var naturalDuration = controller.Clock.NaturalDuration.TimeSpan;
			CurrentTimeTextBlock.Text = currentTime.ToString(@"hh\:mm\:ss");
			TotalTimeTextBlock.Text = naturalDuration.ToString(@"hh\:mm\:ss");
			var ratio = currentTime / naturalDuration;
			TimeSlider.Value = ratio;
		}
	}

	public override void Load(string filePath) {
		if (!File.Exists(filePath)) {
			return;
		}
		FilePath = filePath;
		StatusTextBlock.Text = "Loading".L();
		if (timeline != null && timeline.Source != null && controller != null) {
			playHistory[timeline.Source] = controller.Clock.CurrentTime.GetValueOrDefault();
			controller.Stop();
		}
		var newSource = new Uri(filePath, UriKind.Absolute);
		timeline = new MediaTimeline(newSource) {
			RepeatBehavior = RepeatBehavior.Forever
		};
		timeline.CurrentTimeInvalidated += Timeline_OnCurrentTimeInvalidated;
		var clock = VideoPlayer.Clock = timeline.CreateClock();
		controller = clock.Controller;
		IsOpen = true;
	}

	public override void Close() {
		IsOpen = false;
		var source = VideoPlayer.Source;
		if (source != null && controller != null) {
			playHistory[source] = controller.Clock.CurrentTime.GetValueOrDefault();
			controller.Stop();
		}
		FilePath = null;
		timeline = null;
		controller = null;
	}

	public override void HandleMouseScroll(MouseWheelEventArgs e) {
		if (controller != null && controller.Clock.NaturalDuration.HasTimeSpan) {
			var offset = controller.Clock.CurrentTime.GetValueOrDefault().Add(TimeSpan.FromMilliseconds(-e.Delta * 10));
			if (offset < TimeSpan.Zero) {
				offset = TimeSpan.Zero;
			} else if (offset > controller.Clock.NaturalDuration.TimeSpan) {
				offset = controller.Clock.NaturalDuration.TimeSpan;
			}
			controller?.Seek(offset, TimeSeekOrigin.BeginTime);
		}
	}

	private void VideoPlayer_OnMediaOpened(object sender, RoutedEventArgs e) {
		if (timeline == null || timeline.Source == null) {
			return;
		}
		if (playHistory.TryGetValue(timeline.Source, out var ts)) {
			controller.Seek(ts, TimeSeekOrigin.BeginTime);
		}
		StatusTextBlock.Text = Path.GetFileName(FilePath);
	}
}