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
    // Don't handle if user is typing in an input field
    if (isTypingInInput()) {
        return false;
    }
    
    // Only handle specific keys
    const handledKeys = ['Enter', 'Insert'];
    return handledKeys.includes(event.key);
}