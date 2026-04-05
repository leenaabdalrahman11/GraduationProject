
import React, { useState, useEffect } from 'react';
import './VoiceCommandUI.css';  

const VoiceCommandUI = () => {
  const [message, setMessage] = useState('');
  const [options, setOptions] = useState([]);
  const [status, setStatus] = useState('Loading...');
   const baseUrl = import.meta.env.VITE_API_URL;

  useEffect(() => {
    const fetchData = async () => {
      try {
        const response = await fetch(`${baseUrl}/api/voice/start`);
        const data = await response.json();
        setMessage(data.message);
        setOptions(data.options);
        setStatus(data.status);
      } catch (error) {
        setStatus('Error fetching data');
        console.error('Error fetching data:', error);
      }
    };

    fetchData();
  }, []);

  return (
    <div className="container">
      <div className="voice-command-header">
        <h1>{message}</h1>
        <p>Status: {status}</p>
      </div>

      <div className="buttons">
        {options.map((option, index) => (
          <button key={index} onClick={() => console.log(option)}>
            {option}
          </button>
        ))}
      </div>
    </div>
  );
};

export default VoiceCommandUI;