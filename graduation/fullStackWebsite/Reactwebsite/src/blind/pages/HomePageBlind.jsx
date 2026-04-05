import React, { useState, useRef } from 'react';
import AudioRecorder from '../components/AudioRecorder.jsx';
import StatusDisplay from '../components/StatusDisplay.jsx';
import MainLayout from '../layout/MainLayout.jsx';
import VoiceCommandUI from '../components/VoiceCommandUI.jsx';
  const baseUrl = import.meta.env.VITE_API_URL;


export default function HomePageBlind() {
  const [status, setStatus] = useState("Ready");
  const [recognizedText, setRecognizedText] = useState("-");
  const [screenText, setScreenText] = useState("-");
  const [isMuted, setIsMuted] = useState(false);

  const isMutedRef = useRef(false);
  const recorderRef = useRef(null);
  const streamRef = useRef(null);
  const chunksRef = useRef([]);

  const startRecording = async () => {
    if (isMutedRef.current) {
      setStatus("Muted - recording disabled");
      return;
    }

    try {
      window.speechSynthesis.cancel();

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

      const transcribeRes = await fetch(`${baseUrl}/api/Voice/transcribe`, {
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
    }
  };
  
  

  const sendCommandText = async (text) => {
  try {
    if (!text || text.trim() === "") {
      setStatus("No valid input received");
      setScreenText("Please say something for the search.");
      return;
    }

    setStatus("Processing");

    const res = await fetch(`${baseUrl}/api/voice/command`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ text })
    });

    if (!res.ok) {
      throw new Error("Failed to send command");
    }

    const data = await res.json();

    setScreenText(data.screenText || "-");
    setStatus("Ready");

    if (!isMutedRef.current && data.screenText) {
      window.speechSynthesis.cancel();
      const utterance = new SpeechSynthesisUtterance(data.screenText);
      window.speechSynthesis.speak(utterance);
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
    if (!isMutedRef.current && screenText && screenText !== "-") {
      window.speechSynthesis.cancel();
      const utterance = new SpeechSynthesisUtterance(screenText);
      window.speechSynthesis.speak(utterance);
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
  };

  const handleGoBack = () => {
    window.speechSynthesis.cancel();
    setStatus("Going back to the main menu");
  };
  

  return (
    <MainLayout>
      <div className="grid grid-cols-1 gap-8 lg:grid-cols-[1.2fr_0.9fr]">
        <div className="pt-4">
          <h1 className="mb-8 text-3xl font-bold text-[#0d4f91] md:text-5xl lg:text-6xl">
            {status}
          </h1>

          <StatusDisplay
            status={status}
            recognizedText={recognizedText}
            screenText={screenText}
          />

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
      <VoiceCommandUI/>
    </MainLayout>
  );
}
