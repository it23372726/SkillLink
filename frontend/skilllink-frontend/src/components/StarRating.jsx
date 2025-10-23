// src/components/StarRating.jsx
import React from "react";

const Star = ({ filled, onClick, onMouseEnter, onMouseLeave }) => (
  <button
    type="button"
    className="text-2xl leading-none px-0.5"
    onClick={onClick}
    onMouseEnter={onMouseEnter}
    onMouseLeave={onMouseLeave}
    aria-label={filled ? "star filled" : "star empty"}
    title={String(filled ? "Filled" : "Empty")}
  >
    {filled ? "★" : "☆"}
  </button>
);

const StarRating = ({ value, onChange, size = 5 }) => {
  const [hover, setHover] = React.useState(0);
  const stars = Array.from({ length: size }, (_, i) => i + 1);
  const active = hover || value;

  return (
    <div className="inline-flex items-center">
      {stars.map((n) => (
        <Star
          key={n}
          filled={n <= active}
          onClick={() => onChange(n)}
          onMouseEnter={() => setHover(n)}
          onMouseLeave={() => setHover(0)}
        />
      ))}
      {value > 0 && <span className="ml-2 text-sm">{value}/{size}</span>}
    </div>
  );
};

export default StarRating;
