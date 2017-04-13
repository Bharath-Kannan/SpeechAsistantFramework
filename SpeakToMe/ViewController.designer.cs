// WARNING
//
// This file has been generated automatically by Xamarin Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
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

            if (recordButton != null) {
                recordButton.Dispose ();
                recordButton = null;
            }

            if (recordStatus != null) {
                recordStatus.Dispose ();
                recordStatus = null;
            }

            if (selectedScreenTextView != null) {
                selectedScreenTextView.Dispose ();
                selectedScreenTextView = null;
            }

            if (selectScreenButton != null) {
                selectScreenButton.Dispose ();
                selectScreenButton = null;
            }

            if (textView != null) {
                textView.Dispose ();
                textView = null;
            }
        }
    }
}