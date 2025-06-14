let dotNetRef = null;
let keyDownHandler = null;

export function setupKeyboardHandler(dotNetReference) {
    dotNetRef = dotNetReference;
    
    // Remove existing handler if it exists
    if (keyDownHandler) {
        document.removeEventListener('keydown', keyDownHandler);
    }
    
    // Add new handler
    keyDownHandler = function(event) {
        // Check if we should handle this key event
        if (shouldHandleKeyEvent(event)) {
            dotNetRef.invokeMethodAsync('HandleKeyboardEvent', 
                event.key, 
                event.shiftKey, 
                event.ctrlKey, 
                event.altKey
            );
        }
    };
    
    document.addEventListener('keydown', keyDownHandler);
}

export function cleanup() {
    if (keyDownHandler) {
        document.removeEventListener('keydown', keyDownHandler);
        keyDownHandler = null;
    }
    dotNetRef = null;
}

export function isTypingInInput() {
    const activeElement = document.activeElement;
    if (!activeElement) return false;
    
    const tagName = activeElement.tagName.toLowerCase();
    const type = activeElement.type?.toLowerCase();
    
    // Check if user is typing in an input field
    if (tagName === 'input' && type !== 'button' && type !== 'submit' && type !== 'reset' && type !== 'checkbox' && type !== 'radio') {
        return true;
    }
    
    // Check if user is typing in textarea or contenteditable
    if (tagName === 'textarea' || activeElement.isContentEditable) {
        return true;
    }
    
    return false;
}

function shouldHandleKeyEvent(event) {
    // Only handle specific keys
    const handledKeys = ['Enter', 'Insert'];
    if (!handledKeys.includes(event.key)) {
        return false;
    }
    
    // Don't handle if user is typing in an input field, unless it's specific combinations
    if (isTypingInInput()) {
        // Allow Shift+Enter and Insert even when typing
        if (event.key === 'Enter' && event.shiftKey) {
            return true;
        }
        if (event.key === 'Insert') {
            return true;
        }
        // Don't handle regular Enter when typing (let the input field handle it)
        return false;
    }
    
    return true;
}