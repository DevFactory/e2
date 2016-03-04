package e2.web;

import com.google.common.base.Preconditions;
import com.google.common.base.Throwables;

import org.eclipse.jetty.server.Handler;
import org.eclipse.jetty.server.Server;

import java.util.logging.Logger;

import e2.Constants;
import e2.conf.Configuration;

public abstract class UIWebServer {
    private static final Logger log = Logger.getLogger(UIWebServer.class.getName());

    private final Server server;
    private final Configuration configuration;

    public UIWebServer(Configuration conf) {
        Preconditions.checkNotNull(conf, "Configuration cannot be null.");
        configuration = conf;
        server = new Server(configuration.getInt(Constants.WEB_PORT));
    }

    public void setHandler(Handler handler) {
        server.setHandler(handler);
    }

    public void startWebServer() {
        try {
            server.start();
        } catch (Exception e) {
            throw Throwables.propagate(e);
        }
    }
}
