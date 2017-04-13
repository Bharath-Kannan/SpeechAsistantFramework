
using Android.App;
using Android.Widget;
using Android.OS;
using Android.Views;
using System;
using NaturalLanguageProcessor;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using Android.Content;
using Android.Speech;
using Android.Speech.Tts;

namespace SpeakToMe.Droid
{
    [Activity(Label = "SpeakToMe.Droid", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity, TextToSpeech.IOnInitListener
    {
        public static readonly string[] dropdownLists = { "Global", "SearchResultsScreen", "PropertyDetailsScreen" };
        public NaturalLanguageProcessor.NaturalLanguageProcessor nlp = new NaturalLanguageProcessor.NaturalLanguageProcessor();
        private readonly int VOICE = 1;
        public String ActionName;
        public Dictionary<string, List<string>> parameters = new Dictionary<string, List<string>>();

        protected override void OnCreate(Bundle bundle)
        {
            TextToSpeech textToSpeech;
            var lang = Java.Util.Locale.Default;
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.Main);
            var screenDropDownList = FindViewById<Spinner>(Resource.Id.ScreenList);
            var showListButton = FindViewById<Button>(Resource.Id.btnShowScreen);
            var micButton = FindViewById<ImageButton>(Resource.Id.btnMic);
            var btnSayIt = FindViewById<Button>(Resource.Id.btnSpeak);
            var textView = FindViewById<TextView>(Resource.Id.SpeechTextView);
            textView.Text = "Hello There";
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
                    screenDropDownList.Visibility = ViewStates.Visible;
                }
            };

            micButton.Click += delegate
              {
                  var voiceIntent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
                  voiceIntent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);

                  // put a message on the modal dialog
                 // voiceIntent.PutExtra(RecognizerIntent.ExtraPrompt, "Speak Now");

                  // if there is more then 1.5s of silence, consider the speech over
                  voiceIntent.PutExtra(RecognizerIntent.ExtraSpeechInputCompleteSilenceLengthMillis, 1000);
                  voiceIntent.PutExtra(RecognizerIntent.ExtraSpeechInputPossiblyCompleteSilenceLengthMillis,1000);
                  //voiceIntent.PutExtra(RecognizerIntent.ExtraSpeechInputMinimumLengthMillis, 1000);
                  voiceIntent.PutExtra(RecognizerIntent.ExtraMaxResults, 1);
                  voiceIntent.PutExtra(RecognizerIntent.ExtraLanguage, Java.Util.Locale.Default);
                  StartActivityForResult(voiceIntent, VOICE);
              };

            btnSayIt.Click += delegate
            {
                if (!string.IsNullOrEmpty(ActionName))
                {
                    textToSpeech.Speak("Action Name is", QueueMode.Add, null, null);
                    textToSpeech.Speak(ActionName, QueueMode.Add, null, null);
                }
                if (parameters.Count!=0)
                {
                    textToSpeech.Speak("Parameters are", QueueMode.Add, null, null);
                    foreach (var param in parameters)
                    {    
                        foreach (var item in param.Value)
                        {
                            textToSpeech.Speak(item, QueueMode.Add, null, null);
                        }
                        textToSpeech.Speak(param.Key, QueueMode.Add, null, null);
                    }
                }
            };
        }

        void TextToSpeech.IOnInitListener.OnInit(OperationResult status)
        {
        }

        private void dropdown_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            nlp.SetConfiguration(ReadAssetFile("RPRSpeechIntents.json"), ReadAssetFile("RPRScreenContexts.json"));
            var textView = FindViewById<TextView>(Resource.Id.SpeechTextView);
            var suggestions = nlp.GetSuggestions(dropdownLists[e.Id]);
            var micButton = FindViewById<ImageButton>(Resource.Id.btnMic);
            var btnSayIt = FindViewById<Button>(Resource.Id.btnSpeak);

            if (suggestions != null)
            {
                textView.Text = "Some things you can ask me:";
                suggestions.ForEach(s =>
                {
                    textView.Text = textView.Text + "\r\n"+ s;
                });
                micButton.Visibility = ViewStates.Visible;
            }
            else
            {
                textView.Text = "Sorry, I do not understand the context.";
                ActionName = textView.Text;
                btnSayIt.CallOnClick();
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

        protected override void OnActivityResult(int requestCode, Result resultVal, Intent data)
        {
            var textView = FindViewById<TextView>(Resource.Id.SpeechTextView);
            var btnSayIt = FindViewById<Button>(Resource.Id.btnSpeak);
            var lang = Java.Util.Locale.Default;

            if (requestCode == VOICE)
            {
                if (resultVal == Result.Ok)
                {
                    var matches = data.GetStringArrayListExtra(RecognizerIntent.ExtraResults);
                    if (matches.Count != 0)
                    {
                        textView.Text = matches[0];
                        var intentResult = nlp.GetMatchingIntent(textView.Text);
                        if (intentResult != null)
                        {
                            textView.Text+= "\r\n" +"Awesome, I will get it done.";
                            textView.Text += "\r\n" + "Action: " + intentResult.Action;
                            ActionName = intentResult.Action;
                            if (intentResult.Parameters != null)
                            {
                                foreach (var paramter in intentResult.Parameters)
                                {
                                    var parameterValues = new List<string>();
                                    textView.Text += "\r\n" + "Parameter Name: " + paramter.Key;
                                    textView.Text += "\r\n" + "Parameter Values: " + string.Join(", ", paramter.Value);
                                    parameters.Add(paramter.Key.ToString(), parameterValues);
                                    foreach (var item in paramter.Value)
                                    {
                                        parameterValues.Add(item);
                                    }
                                }
                            }
                            else
                                textView.Text += "\r\n" + "No specific parameters mentioned.";
                            btnSayIt.CallOnClick();
                        }
                        else
                            textView.Text = "Sorry, I do not understand that.";

                    }
                    else
                        textView.Text = "No speech was recognised";
                }
            }

            base.OnActivityResult(requestCode, resultVal, data);
        }
    }
}
