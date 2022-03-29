using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace STTHesitationDetection
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //  var speechConfig = SpeechConfig.FromSubscription("c469ae42cbcd4e718ee389851b068eee", "centralus");        
            // speechConfig.SpeechRecognitionLanguage = "en-US";

            //To recognize speech from an audio file, use `FromWavFileInput` instead of `FromDefaultMicrophoneInput`:
            // using var audioConfig = AudioConfig.FromWavFileInput("intro.wav");
            // // using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            // using var speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);

            // Console.WriteLine("Speak into your microphone.");
            // var speechRecognitionResult = await speechRecognizer.RecognizeOnceAsync();
            
            await ContinuousRecognitionWithFileAsync("intro.wav");
    
        }

        public static async Task<string> ContinuousRecognitionWithFileAsync(string FileName)
        {
            // <recognitionContinuousWithFile>
            // Creates an instance of a speech config with specified subscription key and service region.
            // Replace with your own subscription key and service region (e.g., "westus").
            var config = SpeechConfig.FromSubscription("<your api key>", "<your region>");
            config.OutputFormat = OutputFormat.Detailed;
            config.RequestWordLevelTimestamps();
            var stopRecognition = new TaskCompletionSource<int>();
            
            string sReturn = "";
            // Creates a speech recognizer using file as audio input.
            // Replace with your own audio file name.
            using (var audioInput = AudioConfig.FromWavFileInput(FileName))
            {
                using (var recognizer = new SpeechRecognizer(config, audioInput))
                {
                    WordLevelTimingResult previousWord = null;
                    // Subscribes to events.
                    recognizer.Recognizing += (s, e) =>
                    {
                        Console.WriteLine($"RECOGNIZING: Text={e.Result.Text}");
                        
                        
                        // sReturn += e.Result.Text;
                    };

                    recognizer.Recognized += (s, e) =>
                    {
                        if (e.Result.Reason == ResultReason.RecognizedSpeech)
                        {
                            Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
                            try
                            {
                                List<DetailedSpeechRecognitionResult> theBestResult = e.Result.Best().ToList();
                                // var resultingText = $"{string.Format(CultureInfo.InvariantCulture, e.Result.Text)} [Confidence:{theBestResult[0].Confidence}] \n";                
                                
                                foreach(WordLevelTimingResult word in theBestResult.FirstOrDefault().Words)
                                {
                                    if(previousWord == null)
                                    {
                                        previousWord = word;
                                        continue;
                                    }
                                    long millisecondsBetweenWords = (word.Offset - previousWord.Offset) / TimeSpan.TicksPerMillisecond;
                                    
                                    if(millisecondsBetweenWords >=1000)
                                    {
                                        Console.WriteLine($"Pause greater than 1 second between {previousWord.Word} and {word.Word}");
                                    }
                                    previousWord = word;
                                }
                            }
                            catch(Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                            sReturn += e.Result.Text;
                        }
                        else if (e.Result.Reason == ResultReason.NoMatch)
                        {
                            Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                        }
                    };

                    recognizer.Canceled += (s, e) =>
                    {
                        Console.WriteLine($"CANCELED: Reason={e.Reason}");

                        if (e.Reason == CancellationReason.Error)
                        {
                            Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                            Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                            Console.WriteLine($"CANCELED: Did you update the subscription info?");
                        }

                        stopRecognition.TrySetResult(0);
                    };

                    recognizer.SessionStarted += (s, e) =>
                    {
                        Console.WriteLine("\n    Session started event.");
                    };

                    recognizer.SessionStopped += (s, e) =>
                    {
                        Console.WriteLine("\n    Session stopped event.");
                        Console.WriteLine("\nStop recognition.");
                        stopRecognition.TrySetResult(0);
                    };

                    // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
                    await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                    // Waits for completion.
                    // Use Task.WaitAny to keep the task rooted.
                    Task.WaitAny(new[] { stopRecognition.Task });

                    // Stops recognition.
                    await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                }

                return sReturn;
            }
            // </recognitionContinuousWithFile>
        }
    }
}
