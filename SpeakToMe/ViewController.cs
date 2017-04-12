using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AVFoundation;
using Foundation;
using NaturalLanguageProcessor;
using Newtonsoft.Json;
using Speech;
using UIKit;

namespace SpeakToMe
{
	public partial class ViewController : UIViewController, ISFSpeechRecognizerDelegate
	{
		readonly SFSpeechRecognizer speechRecognizer = new SFSpeechRecognizer (new NSLocale ("en-US"));
		readonly AVAudioEngine audioEngine = new AVAudioEngine ();
		SFSpeechAudioBufferRecognitionRequest recognitionRequest;
		SFSpeechRecognitionTask recognitionTask;
		NaturalLanguageProcessor.NaturalLanguageProcessor nlp;
		System.Timers.Timer speechIdleTimer;


		AVSpeechSynthesizer ss = new AVSpeechSynthesizer ();


		[Outlet ("textView")]
		UITextView textView { get; set; }

		[Outlet ("recordButton")]
		UIButton recordButton { get; set; }

		protected ViewController (IntPtr handle)
			: base (handle) {
		}


		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			ss = new AVSpeechSynthesizer ();

			ss.DidFinishSpeechUtterance += (object sender, AVSpeechSynthesizerUteranceEventArgs e) => {

			};

			ss.DidStartSpeechUtterance += (object sender, AVSpeechSynthesizerUteranceEventArgs e) => {

			};

			ss.WillSpeakRangeOfSpeechString += (object sender, AVSpeechSynthesizerWillSpeakEventArgs e) => {
			};


			nlp = new NaturalLanguageProcessor.NaturalLanguageProcessor ();
			nlp.SetConfiguration (File.ReadAllText ("RPRSpeechIntents.json"), File.ReadAllText ("RPRScreenContexts.json"));

			recordButton.Enabled = false;

			this.pickerView.Model = new ScreensModel (this, nlp.ContextConfigurations.Select(cc => cc.Name).ToList());

			speechIdleTimer = new System.Timers.Timer (3 * 1000);
			speechIdleTimer.Elapsed += (sender, e) => {
				this.stopSpeechRecognition ();
				speechIdleTimer.Stop ();
			};

		}

		public override void ViewDidAppear (bool animated)
		{
			speechRecognizer.Delegate = this;
			SFSpeechRecognizer.RequestAuthorization (authStatus => {
				// The callback may not be called on the main thread. Add an
				// operation to the main queue to update the record button's state.
				NSOperationQueue.MainQueue.AddOperation (() => {
					switch (authStatus) {
					case SFSpeechRecognizerAuthorizationStatus.Authorized:
						recordButton.Enabled = true;
						break;

					case SFSpeechRecognizerAuthorizationStatus.Denied:
						recordButton.Enabled = false;
						recordButton.SetTitle ("User denied access to speech recognition", UIControlState.Disabled);
						break;

					case SFSpeechRecognizerAuthorizationStatus.Restricted:
						recordButton.Enabled = false;
						recordButton.SetTitle ("Speech recognition restricted on this device", UIControlState.Disabled);
						break;

					case SFSpeechRecognizerAuthorizationStatus.NotDetermined:
						recordButton.Enabled = false;
						recordButton.SetTitle ("Speech recognition not yet authorized", UIControlState.Disabled);
						break;
					}
				});
			});
		}

		void StartRecording ()
		{
			// Cancel the previous task if it's running.
			recognitionTask?.Cancel ();
			recognitionTask = null;


			var audioSession = AVAudioSession.SharedInstance ();
			NSError err;
			err = audioSession.SetCategory (AVAudioSessionCategory.PlayAndRecord, AVAudioSessionCategoryOptions.DefaultToSpeaker);
			audioSession.SetMode (AVAudioSession.ModeMeasurement, out err);
			err = audioSession.SetActive (true, AVAudioSessionSetActiveOptions.NotifyOthersOnDeactivation);

			// Configure request so that results are returned before audio recording is finished
			recognitionRequest = new SFSpeechAudioBufferRecognitionRequest {
				ShouldReportPartialResults = true,
			};

			var inputNode = audioEngine.InputNode;
			if (inputNode == null)
				throw new InvalidProgramException ("Audio engine has no input node");


			// A recognition task represents a speech recognition session.
			// We keep a reference to the task so that it can be cancelled.
			recognitionTask = speechRecognizer.GetRecognitionTask (recognitionRequest, (result, error) => {
				var isFinal = false;
				if (result != null) {
					speechIdleTimer.Stop ();
					speechIdleTimer.Start ();

					textView.Text = result.BestTranscription.FormattedString;

					isFinal = result.Final;
				}

				if (error != null || isFinal) {

					if (result != null) {
						var intent = nlp.GetMatchingIntent (result.BestTranscription.FormattedString);

						string resultText;
						if (intent != null) {
							textView.Text += "\nAction is "  + intent.Action + ".";
							resultText = "Action is " + intent.Action + ". ";
							if (intent.Parameters != null) {
								intent.Parameters.ForEach (p => {
									resultText += "Parameter " + p.Key + " with values" + string.Join (",", p.Value) + ". ";
									textView.Text += "\nParameter " + p.Key + " with values " + string.Join (",", p.Value) + ". ";
								});
							}
						}
						else
							resultText = "Sorry, I did not get that.";

						var su = new AVSpeechUtterance (resultText) {
							Rate = AVSpeechUtterance.MaximumSpeechRate / 2,
							Voice = AVSpeechSynthesisVoice.FromLanguage ("en-US"),
							PitchMultiplier = 1.0f,
							Volume = 1
						};

						ss.SpeakUtterance (su);
					}

					audioEngine.Stop ();
					inputNode.RemoveTapOnBus (0);
					recognitionRequest = null;
					recognitionTask = null;
					recordButton.Enabled = true;
					//recordButton.SetTitle ("Start Recording", UIControlState.Normal);
					recordButton.Hidden = false;
					recordStatus.Hidden = true;
					speechIdleTimer.Stop ();
				}
			});

			var recordingFormat = inputNode.GetBusOutputFormat (0);
			inputNode.InstallTapOnBus (0, 1024, recordingFormat, (buffer, when) => {
				recognitionRequest?.Append (buffer);
			});

			audioEngine.Prepare ();
			audioEngine.StartAndReturnError (out err);
		}

		#region ISFSpeechRecognizerDelegate

		[Export ("speechRecognizer:availabilityDidChange:")]
		public void AvailabilityDidChange (SFSpeechRecognizer speechRecognizer, bool available)
		{
			if (available) {
				recordButton.Enabled = true;
				//recordButton.SetTitle ("Start Recording", UIControlState.Normal);
			} else {
				recordButton.Enabled = false;
				//recordButton.SetTitle ("Recognition not available", UIControlState.Disabled);
			}
		}

		#endregion

		#region Interface Builder actions

		[Action ("recordButtonTapped")]
		void RecordButtonTapped ()
		{
			if (audioEngine.Running) {
				audioEngine.Stop ();
				recognitionRequest?.EndAudio ();
				recordButton.Enabled = false;
				//recordButton.SetTitle ("Stopping", UIControlState.Disabled);
			} else {
				StartRecording ();
				recordButton.Hidden = true;
				recordStatus.Hidden = false;
				speechIdleTimer.Start ();
			}
		}

		private void stopSpeechRecognition () {
			audioEngine.Stop ();
			recognitionRequest?.EndAudio ();
			recordButton.Enabled = false;
			//recordButton.SetTitle ("Stopping", UIControlState.Disabled);
			recordButton.Hidden = false;
			recordStatus.Hidden = true;
		}

		partial void selectScreenButton_Click (Foundation.NSObject sender) {
			this.pickerView.Hidden = false;
		}

		#endregion

		public void SelectScreen (string screenName) {
			var suggestions = this.nlp.GetSuggestions (screenName);

			textView.Text = string.Join ("\n", suggestions);
			this.selectedScreenTextView.Text = screenName;

			this.pickerView.Hidden = true;
		}
	}

	public class ScreensModel : UIPickerViewModel {
		private List<string> screens;

		ViewController viewController;

		public ScreensModel (ViewController viewController, List<string> screens) {
			this.viewController = viewController;
			this.screens = screens;
		}

		public override nint GetComponentCount (UIPickerView v) {
			return 1;
		}

		public override nint GetRowsInComponent (UIPickerView pickerView, nint component) {
			return screens.Count;
		}

		public override string GetTitle (UIPickerView picker, nint row, nint component) {
			switch (component) {
			case 0:
				return screens [(int)row];
			case 1:
				return row.ToString ();
			case 2:
				return new string ((char)('A' + row), 1);
			default:
				throw new NotImplementedException ();
			}
		}

		public override void Selected (UIPickerView picker, nint row, nint component) {
			this.viewController.SelectScreen (screens [(int)row]);
		}

		public override nfloat GetComponentWidth (UIPickerView picker, nint component) {
			if (component == 0)
				return 220f;
			else
				return 30f;
		}
	}

}
