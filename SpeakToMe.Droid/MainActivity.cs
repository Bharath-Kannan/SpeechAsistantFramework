using Android.App;
using Android.Widget;
using Android.OS;
using Android.Views;
using System;
using System.Collections.Generic;
using System.IO;
using Android.Content;
using Android.Speech;
using Android.Speech.Tts;
using Android.Runtime;
using Newtonsoft.Json;
using NaturalLanguageProcessor;
using System.Linq;
using Android.Graphics.Drawables;
using Android.Graphics;

namespace SpeakToMe.Droid
{
    [Activity(Label = "SpeakToMe", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity, TextToSpeech.IOnInitListener, IRecognitionListener
    {
        #region Global variables

        public NaturalLanguageProcessor.NaturalLanguageProcessor nlp = new NaturalLanguageProcessor.NaturalLanguageProcessor();
        public List<string> dropdownLists;
        public String speakText;
        #endregion

        protected override void OnCreate(Bundle bundle)
        {
            SetContentView(Resource.Layout.Main);
            nlp.SetConfiguration(ReadAssetFile("RPRSpeechIntents.json"), ReadAssetFile("RPRScreenContexts.json"));

            #region Initialization
            TextToSpeech textToSpeech;
            SpeechRecognizer mSpeechRecognizer;
            Intent mSpeechRecognizerIntent;
            var lang = Java.Util.Locale.Default;
            base.OnCreate(bundle);
            var screenDropDownList = FindViewById<Spinner>(Resource.Id.ScreenList);
            var showListButton = FindViewById<Button>(Resource.Id.btnShowScreen);
            var micButton = FindViewById<ImageButton>(Resource.Id.btnMic);
            var btnSayIt = FindViewById<Button>(Resource.Id.btnSpeak);
            var textView = FindViewById<TextView>(Resource.Id.SpeechTextView);
            var voiceImage = FindViewById<Android.Webkit.WebView>(Resource.Id.voiceWebView);
            dropdownLists = nlp.ContextConfigurations.Select(cc => cc.Name).ToList();
            dropdownLists.Insert(0, "<Select Screen..>");
            voiceImage.Visibility = ViewStates.Invisible;
            textView.Text = "Hello There! Kindly Select a Screen!";
            btnSayIt.Visibility = ViewStates.Invisible;
            micButton.Visibility = ViewStates.Invisible;
            textToSpeech = new TextToSpeech(this, this, "com.google.android.tts");
            textToSpeech.SetLanguage(lang);
            screenDropDownList.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(dropdown_ItemSelected);
            screenDropDownList.Visibility = ViewStates.Invisible;
            var spinnerArrayAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SelectDialogSingleChoice, dropdownLists);
            spinnerArrayAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerItem);
            screenDropDownList.Adapter = spinnerArrayAdapter;
            screenDropDownList.SetSelection(-1);
            #endregion

            #region ButtonClickEvents
            showListButton.Click += delegate
            {

                if (screenDropDownList.Visibility == ViewStates.Visible)
                {
                    showListButton.Text = "Select Screen";
                    screenDropDownList.Visibility = ViewStates.Invisible;
                }
                else
                {
                    showListButton.Text = "Hide Screen";
                    textView.Text = "Hello There! Kindly Select a Screen!";
                    screenDropDownList.Visibility = ViewStates.Visible;
                }
            };

            micButton.Click += delegate
            {
                mSpeechRecognizer = SpeechRecognizer.CreateSpeechRecognizer(this);
                mSpeechRecognizerIntent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
                mSpeechRecognizerIntent.PutExtra(RecognizerIntent.ExtraCallingPackage, PackageName);
                mSpeechRecognizerIntent.PutExtra(RecognizerIntent.ExtraSpeechInputCompleteSilenceLengthMillis, 1000);
                mSpeechRecognizerIntent.PutExtra(RecognizerIntent.ExtraSpeechInputPossiblyCompleteSilenceLengthMillis, 1000);
                mSpeechRecognizerIntent.PutExtra(RecognizerIntent.ExtraMaxResults, 1);
                mSpeechRecognizerIntent.PutExtra(RecognizerIntent.ExtraLanguageModel, this.Resources.Configuration.Locale.Language);
                mSpeechRecognizer.SetRecognitionListener(this);
                mSpeechRecognizer.StartListening(mSpeechRecognizerIntent);
            };

            btnSayIt.Click += delegate
            {
                if (!string.IsNullOrEmpty(speakText))
                    textToSpeech.Speak(speakText, QueueMode.Flush, null, null);
            };
            #endregion
        }

        private void dropdown_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            var textView = FindViewById<TextView>(Resource.Id.SpeechTextView);
            var suggestions = nlp.GetSuggestions(dropdownLists[Convert.ToInt32(e.Id)]);
            var micButton = FindViewById<ImageButton>(Resource.Id.btnMic);
            var btnSayIt = FindViewById<Button>(Resource.Id.btnSpeak);

            if (suggestions != null && e.Id > 0)
            {
                textView.Text = "Some things you can ask me:";
                suggestions.ForEach(s =>
                {
                    textView.Text = textView.Text + "\r\n" + s;
                });
                micButton.Visibility = ViewStates.Visible;
            }
        }

        private string ReadAssetFile(string fileName)
        {
            var file = this.Assets.Open(fileName);
            var fileData = string.Empty;
            using (var test = new StreamReader(file))
            {
                fileData = test.ReadToEnd();
            }

            return fileData;
        }

        #region Interface Implementation

        void TextToSpeech.IOnInitListener.OnInit(OperationResult status)
        {
        }

        public void OnBeginningOfSpeech()
        {
            var webView = FindViewById<Android.Webkit.WebView>(Resource.Id.voiceWebView);
            String data = "<html><head><meta name=\"viewport\"\"content=\"width=100%, initial-scale=0.65 \" /></head>";
            data = data + "<body><center><img width=\"100%\" height=\"100%\" src=\"voice.gif\" /></center></body></html>";
            webView.LoadDataWithBaseURL("file:///android_asset/", data, "text/html", "utf-8", null);
            webView.Visibility = ViewStates.Visible;
        }
        


        public void OnBufferReceived(byte[] buffer)
        {

        }

        public void OnEndOfSpeech()
        {
            var webView = FindViewById<Android.Webkit.WebView>(Resource.Id.voiceWebView);
            webView.Visibility = ViewStates.Invisible;
        }

        public void OnError([GeneratedEnum] SpeechRecognizerError error)
        {
            var webView = FindViewById<Android.Webkit.WebView>(Resource.Id.voiceWebView);
            webView.Visibility = ViewStates.Invisible;
        }

        public void OnEvent(int eventType, Bundle @params)
        {

        }

        public void OnPartialResults(Bundle partialResults)
        {

        }

        public void OnReadyForSpeech(Bundle @params)
        {

        }

        public void OnResults(Bundle results)
        {
            var textView = FindViewById<TextView>(Resource.Id.SpeechTextView);
            var btnSayIt = FindViewById<Button>(Resource.Id.btnSpeak);
            var lang = Java.Util.Locale.Default;

            var matches = results.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
            if (matches.Count != 0)
            {
                textView.Text = matches[0];
                var intentResult = nlp.GetMatchingIntent(textView.Text);
                if (intentResult != null)
                {
                    textView.Text += "\r\n" + "Awesome, I will get it done.";
                    textView.Text += "\r\n" + "Action: " + intentResult.Action;
                    speakText = "Action Name is " + intentResult.Action;
                    if (intentResult.Parameters != null)
                    {
                        var speakTextBuilder = new System.Text.StringBuilder();
                        speakTextBuilder.Append(speakText);
                        var textViewBuilder = new System.Text.StringBuilder();
                        textViewBuilder.Append(textView.Text);
                        foreach (var paramter in intentResult.Parameters)
                        {
                            textViewBuilder.Append("\r\n" + "Parameter Name: " + paramter.Key);
                            textViewBuilder.Append("\r\n" + "Parameter Values: " + string.Join(", ", paramter.Value));
                            speakTextBuilder.Append(" with parameter as " + paramter.Key);
                            foreach (var item in paramter.Value)
                            {
                                speakTextBuilder.Append(" " + item);
                            }
                        }
                        textView.Text = textViewBuilder.ToString();
                        speakText = speakTextBuilder.ToString();
                    }
                    else
                        textView.Text += "\r\n" + "No specific parameters mentioned.";

                    btnSayIt.CallOnClick();
                }
                else
                {
                    textView.Text = "Sorry, I do not understand that... Can You Repeat";
                    speakText = "Sorry, I do not understand that";
                    btnSayIt.CallOnClick();
                }
            }
            else
            {
                textView.Text = "No speech was recognised";
                speakText = "No speech was recognised";
                btnSayIt.CallOnClick();
            }
        }

        public void OnRmsChanged(float rmsdB)
        {
        }

        #endregion
    }
}
