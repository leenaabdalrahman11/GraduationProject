import React from 'react';
const StatusDisplay = ({ status, recognizedText, screenText }) => {
  return (
    <div className="rounded-[24px] bg-white p-5 shadow-sm ring-1 ring-black/5 md:p-6">
      <p className="mb-3 text-lg text-slate-700 md:text-2xl">
        <strong>Status:</strong> {status}
      </p>
      <p className="mb-3 text-lg text-slate-700 md:text-2xl">
        <strong>Recognized text:</strong> {recognizedText}
      </p>
      <p className="text-lg text-slate-700 md:text-2xl">
        <strong>Screen message:</strong> {screenText}
      </p>
    </div>
  );
};
export default StatusDisplay;



