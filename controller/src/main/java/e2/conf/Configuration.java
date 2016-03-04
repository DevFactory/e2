package e2.conf;

import com.google.common.base.Preconditions;

import java.io.IOException;
import java.io.InputStream;
import java.util.Map;
import java.util.Properties;

import e2.exception.ExceptionMessage;

public final class Configuration {
    public static final String DEFAULT_PROPERTIES = "elasticedge-default.properties";
    private final Properties properties = new Properties();

    public Configuration(Map<String, String> props) {
        if (props != null) {
            properties.putAll(props);
        }
    }

    public Configuration(Properties props) {
        if (props != null) {
            properties.putAll(props);
        }
    }

    public Configuration() {
        this(true);
    }

    public Configuration(boolean includeSystemProperties) {
        Properties defaultProps = new Properties();

        InputStream defaultInputStream = Configuration.class.getClassLoader().getResourceAsStream(DEFAULT_PROPERTIES);
        if (defaultInputStream == null) {
            throw new RuntimeException(ExceptionMessage.DEFAULT_PROPERTIES_FILE_DOES_NOT_EXIST.getMessage());
        }

        try {
            defaultProps.load(defaultInputStream);
        } catch (IOException e) {
            throw new RuntimeException(ExceptionMessage.UNABLE_TO_LOAD_PROPERTIES_FILE.getMessage());
        }

        properties.putAll(defaultProps);
    }

    public void set(String key, String value) {
        Preconditions.checkArgument(key != null && value != null,
                String.format("the key value pair (%s, %s) cannot have null", key, value));
        properties.put(key, value);
    }

    public String get(String key) {
        if (!properties.containsKey(key)) {
            throw new RuntimeException(ExceptionMessage.INVALID_CONFIGURATION_KEY.getMessage(key));
        }
        return properties.getProperty(key);
    }

    public int getInt(String key) {
        if (!properties.containsKey(key)) {
            throw new RuntimeException(ExceptionMessage.INVALID_CONFIGURATION_KEY.getMessage(key));
        }
        String value = properties.getProperty(key);
        return Integer.parseInt(value);
    }
}
