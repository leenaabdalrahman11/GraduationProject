import React from 'react';

const AudioRecorder = ({
  startRecording,
  stopRecording,
  handleMute,
  handleReListen,
  handleSkip,
  handleExit,
  handleGoBack,
  isMuted
}) => {
  return (
    <div className="flex gap-4 flex-wrap">
      <button
  onClick={startRecording}
  disabled={isMuted}
  className={`rounded-[28px] border-[3px] px-4 py-4 text-xl font-bold text-[#0d4f91] ${
    isMuted
      ? 'cursor-not-allowed border-gray-400 bg-gray-300'
      : 'border-[#2d71b8] bg-[#7fb3e6]'
  }`}
>
  speak
</button>

      <button
        onClick={stopRecording}
        className="rounded-[28px] border-[3px] border-[#2d71b8] bg-[#bfc3c8] px-4 py-4 text-xl font-bold text-[#0d4f91]"
      >
        Stop
      </button>

      <button
        onClick={handleMute}
        className="rounded-[28px] border-[3px] border-[#2d71b8] bg-[#bfc3c8] px-4 py-4 text-xl font-bold text-[#0d4f91]"
      >
        {isMuted ? "Unmute" : "Mute"}
      </button>

      <button
        onClick={handleReListen}
        className="rounded-[28px] border-[3px] border-[#2d71b8] bg-[#7fb3e6] px-4 py-4 text-xl font-bold text-[#0d4f91]"
      >
        Re listen
      </button>

      <button
        onClick={handleSkip}
        className="rounded-[28px] border-[3px] border-[#2d71b8] bg-[#bfc3c8] px-4 py-4 text-xl font-bold text-[#0d4f91]"
      >
        Skip
      </button>

      <button
        onClick={handleExit}
        className="rounded-[28px] border-[3px] border-[#2d71b8] bg-[#7fb3e6] px-4 py-4 text-xl font-bold text-[#0d4f91]"
      >
        Exit
      </button>

      <button
        onClick={handleGoBack}
        className="rounded-[28px] border-[3px] border-[#2d71b8] bg-[#bfc3c8] px-4 py-4 text-xl font-bold text-[#0d4f91]"
      >
        Go Back
      </button>
    </div>
  );
};

export default AudioRecorder;