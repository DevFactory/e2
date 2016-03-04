package e2.web;

import com.google.common.base.Preconditions;

import org.eclipse.jetty.server.Handler;
import org.eclipse.jetty.server.handler.ContextHandler;
import org.eclipse.jetty.server.handler.ContextHandlerCollection;

import e2.Controller;
import e2.conf.Configuration;

public class ControllerUIWebServer extends UIWebServer {
    public ControllerUIWebServer(Controller controller, Configuration conf) {
        super(conf);
        Preconditions.checkNotNull(controller, "Controller cannot be null.");

        ContextHandler contextGeneral = new ContextHandler("/");
        contextGeneral.setHandler(new WebInterfaceGeneralHandler(controller));

        ContextHandlerCollection contexts = new ContextHandlerCollection();
        contexts.setHandlers(new Handler[]{contextGeneral});

        this.setHandler(contexts);
    }
}
