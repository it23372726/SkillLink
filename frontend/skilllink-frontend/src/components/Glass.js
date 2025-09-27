export const GlassCard = ({ className = "", children }) => (
    <div className={`glass ${className}`}>{children}</div>
  );
  
  export const GlassBar = ({ className = "", children }) => (
    <div className={`glass-bar ${className}`}>{children}</div>
  );
  
  export const MacButton = ({ className = "", children, ...props }) => (
    <button className={`mac-btn ${className}`} {...props}>{children}</button>
  );
  
  export const MacPrimary = (props) => <MacButton className="mac-btn-primary" {...props} />;
  export const MacDanger = (props) => <MacButton className="mac-btn-danger" {...props} />;
  
  export const MacToggle = ({ checked, onChange }) => (
    <button
      type="button"
      data-checked={checked}
      onClick={onChange}
      aria-pressed={checked}
      className="mac-toggle"
    >
      <span className="mac-thumb" />
    </button>
  );
  