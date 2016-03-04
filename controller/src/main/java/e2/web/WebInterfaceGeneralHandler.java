package e2.web;

import org.eclipse.jetty.server.Request;
import org.eclipse.jetty.server.handler.AbstractHandler;

import java.io.IOException;
import java.io.PrintWriter;

import javax.servlet.ServletException;
import javax.servlet.http.HttpServletRequest;
import javax.servlet.http.HttpServletResponse;

import e2.Controller;


public final class WebInterfaceGeneralHandler extends AbstractHandler {

    private final Controller controller;

    public WebInterfaceGeneralHandler(Controller ctrl) {
        controller = ctrl;
    }

    @Override
    public void handle(String target, Request baseRequest, HttpServletRequest request, HttpServletResponse response)
            throws IOException, ServletException {
        PrintWriter out = response.getWriter();
        out.println("hello world.");
        baseRequest.setHandled(true);
    }
}
