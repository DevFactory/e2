package e2.exception;

import com.google.common.base.Preconditions;

import java.text.MessageFormat;

public enum ExceptionMessage {
    // Conf
    DEFAULT_PROPERTIES_FILE_DOES_NOT_EXIST("The default E2 properties file does not exist"),
    UNABLE_TO_LOAD_PROPERTIES_FILE("Unable to load default E2 properties file"),
    INVALID_CONFIGURATION_KEY("Invalid configuration key {0}"),

    // Semicolon
    ;

    private final MessageFormat message;

    ExceptionMessage(String msg) {
        message = new MessageFormat(msg);
    }

    public String getMessage(Object... params) {
        Preconditions.checkArgument(message.getFormats().length == params.length, "The message takes "
                + message.getFormats().length + " arguments, but is given " + params.length);

        synchronized (message) {
            return message.format(params);
        }
    }
}
