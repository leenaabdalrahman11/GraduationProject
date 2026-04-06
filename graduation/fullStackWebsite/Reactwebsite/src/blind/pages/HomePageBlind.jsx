import React, { useState, useRef, useEffect } from 'react';
import AudioRecorder from '../components/AudioRecorder.jsx';
import MainLayout from '../layout/MainLayout.jsx';

const baseUrl = import.meta.env.VITE_API_URL;

export default function HomePageBlind() {
  const [status, setStatus] = useState("Ready");
  const [recognizedText, setRecognizedText] = useState("-");
  const [screenText, setScreenText] = useState("-");
  const [isMuted, setIsMuted] = useState(false);

  const [message, setMessage] = useState('');
  const [options, setOptions] = useState([]);
  const [voiceStartStatus, setVoiceStartStatus] = useState('Loading...');
  const [selectedLanguage, setSelectedLanguage] = useState("");
  const [languageStep, setLanguageStep] = useState(true);
  const [lastSpokenText, setLastSpokenText] = useState("");

  const isMutedRef = useRef(false);
  const recorderRef = useRef(null);
  const streamRef = useRef(null);
  const chunksRef = useRef([]);

  useEffect(() => {
    if (languageStep && !isMutedRef.current) {
      window.speechSynthesis.cancel();
      const utterance = new SpeechSynthesisUtterance("Please say Arabic or English.");
      window.speechSynthesis.speak(utterance);
    }
  }, [languageStep]);
useEffect(() => {
  const loadVoices = () => {
    window.speechSynthesis.getVoices();
  };

  loadVoices();

  if (window.speechSynthesis.onvoiceschanged !== undefined) {
    window.speechSynthesis.onvoiceschanged = loadVoices;
  }

  return () => {
    window.speechSynthesis.onvoiceschanged = null;
  };
}, []);
  useEffect(() => {
    const fetchVoiceStartData = async () => {
      try {
        const response = await fetch(`${baseUrl}/api/voice/start`);
        const data = await response.json();

        setMessage(data.message || '');
        setOptions(data.options || []);
        setVoiceStartStatus(data.status || 'Ready');
      } catch (error) {
        setVoiceStartStatus('Error fetching data');
        console.error('Error fetching data:', error);
      }
    };

    fetchVoiceStartData();
  }, []);

const speakText = (text) => {
  if (!text || isMutedRef.current) return;
  if (!("speechSynthesis" in window)) return;

  const synth = window.speechSynthesis;
  synth.cancel();
  synth.resume();

  const speakNow = () => {
    const utterance = new SpeechSynthesisUtterance(text);
    const hasArabic = /[\u0600-\u06FF]/.test(text);
    const voices = synth.getVoices();

    console.log("Available voices:", voices.map(v => `${v.name} - ${v.lang}`));
    console.log("Text to speak:", text);

    let selectedVoice = null;

    if (hasArabic || selectedLanguage === "ar") {
      utterance.lang = "ar";
      selectedVoice =
        voices.find((v) => v.lang && v.lang.toLowerCase().startsWith("ar")) ||
        voices[0] ||
        null;
    } else {
      utterance.lang = "en-US";
      selectedVoice =
        voices.find((v) => v.lang && v.lang.toLowerCase().startsWith("en")) ||
        voices[0] ||
        null;
    }

    if (selectedVoice) {
      utterance.voice = selectedVoice;
      console.log("Selected voice:", selectedVoice.name, selectedVoice.lang);
    }

    utterance.rate = 1;
    utterance.pitch = 1;
    utterance.volume = 1;

    utterance.onstart = () => console.log("Speech started");
    utterance.onend = () => console.log("Speech ended");
    utterance.onerror = (e) => console.error("Speech error:", e);

    synth.speak(utterance);
  };
console.log(window.speechSynthesis.getVoices());
  setTimeout(speakNow, 300);
};

  const handleLanguageSelect = (lang) => {
    const isArabic = lang === "ar";

    setSelectedLanguage(lang);
    setLanguageStep(false);
    setStatus("Language selected");
    setRecognizedText(isArabic ? "Arabic" : "English");
    setScreenText(isArabic ? "تم اختيار العربية" : "English selected");

    speakText(isArabic ? "تم اختيار العربية" : "English selected");
  };

  const startRecording = async () => {
    if (isMutedRef.current) {
      setStatus("Muted - recording disabled");
      return;
    }

    try {
      window.speechSynthesis.cancel();
      await new Promise((resolve) => setTimeout(resolve, 400));

      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      streamRef.current = stream;
      chunksRef.current = [];

      const recorder = new MediaRecorder(stream, { mimeType: 'audio/webm' });
      recorderRef.current = recorder;

      recorder.onstart = () => setStatus("Listening");

      recorder.ondataavailable = (event) => {
        if (event.data.size > 0) {
          chunksRef.current.push(event.data);
        }
      };

      recorder.onstop = async () => {
        const blob = new Blob(chunksRef.current, { type: 'audio/webm' });
        await transcribeAndSend(blob);
      };

      recorder.start();
    } catch (error) {
      console.error(error);
      setStatus("Recording error");
    }
  };

  const stopRecording = () => {
    if (recorderRef.current && recorderRef.current.state !== "inactive") {
      recorderRef.current.stop();
    }

    if (streamRef.current) {
      streamRef.current.getTracks().forEach((track) => track.stop());
    }
  };

  const transcribeAndSend = async (audioBlob) => {
    try {
      setStatus("Uploading audio");

      const file = new File([audioBlob], "voice.webm", { type: "audio/webm" });
      const formData = new FormData();
      formData.append("file", file);
      formData.append("language", selectedLanguage || "ar");

      const transcribeRes = await fetch(`${baseUrl}/api/voice/transcribe`, {
        method: "POST",
        body: formData
      });

      if (!transcribeRes.ok) {
        throw new Error("Failed to transcribe audio");
      }

      const transcribeData = await transcribeRes.json();
      const text = transcribeData.text || "";

      setRecognizedText(text || "-");
      await sendCommandText(text);
    } catch (error) {
      console.error(error);
      setStatus("Transcription error");
      setScreenText("Could not transcribe audio.");
    }
  };

const sendCommandText = async (text) => {
  try {
    if (languageStep) {
      const lowerText = text.toLowerCase().trim();

      if (
        lowerText.includes("arabic") ||
        lowerText.includes("عربي") ||
        lowerText.includes("العربية")
      ) {
        handleLanguageSelect("ar");
        return;
      }

      if (
        lowerText.includes("english") ||
        lowerText.includes("انجليزي") ||
        lowerText.includes("english language")
      ) {
        handleLanguageSelect("en");
        return;
      }

      setScreenText("Please say Arabic or English.");
      setRecognizedText(text || "-");
      speakText("Please say Arabic or English.");
      return;
    }

    if (!text || text.trim() === "") {
      setStatus("No valid input received");
      setScreenText("Please say something for the search.");
      return;
    }

    setStatus("Processing");

    const res = await fetch(`${baseUrl}/api/voice/command`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        text,
        language: selectedLanguage
      })
    });

    if (!res.ok) {
      throw new Error("Failed to send command");
    }

    const data = await res.json();

    setRecognizedText(data.correctedText || data.recognizedText || text || "-");
    setScreenText(data.screenText || "-");
    setStatus("Ready");

const textToSpeak = data.replyText || data.screenText || "";
setLastSpokenText(textToSpeak);

if (!isMutedRef.current && textToSpeak) {
  speakText(textToSpeak);
}

  } catch (error) {
    console.error(error);
    setStatus("Error sending command");
    setScreenText("Could not send command to API.");
  }
};

  const handleMute = () => {
    const nextMutedState = !isMutedRef.current;

    isMutedRef.current = nextMutedState;
    setIsMuted(nextMutedState);

    if (nextMutedState) {
      window.speechSynthesis.cancel();
      setStatus("Muted");
    } else {
      setStatus("Unmuted");
    }
  };

const handleReListen = () => {
  if (!isMutedRef.current && lastSpokenText) {
    speakText(lastSpokenText);
    setStatus("Replaying audio");
  }
};

  const handleSkip = () => {
    window.speechSynthesis.cancel();
    setStatus("Skipped audio");
  };

  const handleExit = () => {
    window.speechSynthesis.cancel();

    if (recorderRef.current && recorderRef.current.state !== "inactive") {
      recorderRef.current.stop();
    }

    if (streamRef.current) {
      streamRef.current.getTracks().forEach((track) => track.stop());
    }

    setStatus("Exited");
    setRecognizedText("-");
    setScreenText("-");
    setSelectedLanguage("");
    setLanguageStep(true);
  };

  const handleGoBack = () => {
    window.speechSynthesis.cancel();
    setStatus("Going back to the main menu");
  };

  return (
    <MainLayout>
      <div className="min-h-screen bg-[#f6efe7] px-4 py-6 md:px-8">
        <div className="mx-auto max-w-7xl rounded-[32px] border-2 border-[#b08968] bg-[#efe3d3] p-6 shadow-lg md:p-8">
          <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">

            <div className="rounded-[24px] border-2 border-[#c6a58b] bg-[#f9f3ed] p-6 shadow-sm">
              <h1 className="mb-3 text-3xl font-bold leading-snug text-[#6b4226] md:text-5xl">
                {languageStep
                  ? "Choose language"
                  : (message || "Hi, How can I help you ...")}
              </h1>

              <p className="mb-5 text-base font-semibold text-[#8b5e3c] md:text-lg">
                Status: {languageStep ? "Waiting for language selection" : voiceStartStatus}
              </p>

              <div className="rounded-[20px] border border-[#d6bda7] bg-[#fffaf6] px-4 py-3">
                <p className="text-sm font-semibold text-[#8b5e3c] md:text-base">
                  User said:
                </p>
                <p className="mt-1 break-words text-lg font-bold text-[#6b4226] md:text-2xl">
                  {recognizedText && recognizedText !== "-" ? recognizedText : "No speech detected yet"}
                </p>
              </div>
            </div>

            <div className="rounded-[24px] border-2 border-[#c6a58b] bg-[#f9f3ed] p-5 shadow-sm">
              <div className="mb-4">
                <h2 className="text-2xl font-bold text-[#6b4226]">
                  {languageStep ? "Choose Language" : "Voice Commands"}
                </h2>
                <p className="mt-1 text-sm text-[#8b5e3c]">
                  {languageStep
                    ? "Select a language or say it by voice"
                    : "Choose one of the available options"}
                </p>
              </div>

              {languageStep ? (
                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                  <button
                    onClick={() => handleLanguageSelect("ar")}
                    className="w-full rounded-[22px] border-2 border-[#8b5e3c] bg-[#d8c2ad] px-6 py-5 text-center text-xl font-bold text-[#6b4226] transition hover:bg-[#c9ae95]"
                  >
                    Arabic
                  </button>

                  <button
                    onClick={() => handleLanguageSelect("en")}
                    className="w-full rounded-[22px] border-2 border-[#8b5e3c] bg-[#d8c2ad] px-6 py-5 text-center text-xl font-bold text-[#6b4226] transition hover:bg-[#c9ae95]"
                  >
                    English
                  </button>
                </div>
              ) : (
                <div className="flex flex-col gap-4">
                  {options.map((option, index) => (
                    <button
                      key={index}
                      onClick={() => sendCommandText(option)}
                      className="w-full rounded-[22px] border-2 border-[#8b5e3c] bg-[#d8c2ad] px-6 py-4 text-left text-xl font-bold text-[#6b4226] transition hover:bg-[#c9ae95]"
                    >
                      {option}
                    </button>
                  ))}
                </div>
              )}
            </div>

            <div className="lg:col-span-2 rounded-[24px] border-2 border-[#c6a58b] bg-[#f9f3ed] p-4 shadow-sm">
              <AudioRecorder
                startRecording={startRecording}
                stopRecording={stopRecording}
                handleMute={handleMute}
                handleReListen={handleReListen}
                handleSkip={handleSkip}
                handleExit={handleExit}
                handleGoBack={handleGoBack}
                isMuted={isMuted}
              />
            </div>

          </div>
        </div>
      </div>
    </MainLayout>
  );
}