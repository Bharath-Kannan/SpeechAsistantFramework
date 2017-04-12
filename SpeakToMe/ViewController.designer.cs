// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace SpeakToMe
{
	[Register ("ViewController")]
	partial class ViewController
	{
		[Outlet]
		UIKit.UIPickerView pickerView { get; set; }

		[Outlet]
		UIKit.UITextView recordStatus { get; set; }

		[Outlet]
		UIKit.UITextView selectedScreenTextView { get; set; }

		[Outlet]
		UIKit.UIButton selectScreenButton { get; set; }

		[Action ("selectScreenButton_Click:")]
		partial void selectScreenButton_Click (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (pickerView != null) {
				pickerView.Dispose ();
				pickerView = null;
			}

			if (selectedScreenTextView != null) {
				selectedScreenTextView.Dispose ();
				selectedScreenTextView = null;
			}

			if (selectScreenButton != null) {
				selectScreenButton.Dispose ();
				selectScreenButton = null;
			}

			if (recordStatus != null) {
				recordStatus.Dispose ();
				recordStatus = null;
			}
		}
	}
}
