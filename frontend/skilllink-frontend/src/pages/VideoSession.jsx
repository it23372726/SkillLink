import React from "react";

const VideoSession = ({ roomName }) => {
  const domain = "meet.jit.si"; // Free Jitsi server
  const url = `https://${domain}/${roomName}`;

  return (
    <iframe
      src={url}
      style={{ width: "100%", height: "600px", border: 0 }}
      allow="camera; microphone; fullscreen; display-capture"
      title="Jitsi Meeting"
    />
  );
};

export default VideoSession;
