package e2;

import java.io.IOException;
import java.util.concurrent.ExecutionException;

import e2.agent.NotificationAgent;
import e2.agent.ServerAgent;
import e2.pipelet.PipeletManager;

public class Master {
    private static void printLogo() {
        System.out.println("      __         __  _              __       ");
        System.out.println(" ___ / /__ ____ / /_(_)___  ___ ___/ /__ ____ ");
        System.out.println("/ -_) / _ `(_-</ __/ / __/ / -_) _  / _ `/ -_)");
        System.out.println("\\__/_/\\_,_/___/\\__/_/\\__/  \\__/\\_,_/\\_, /\\__/ ");
        System.out.println("                                   /___/    ");
    }

    public static void run() throws IOException, ExecutionException {
        ServerAgent sa = new ServerAgent();
        NotificationAgent na = new NotificationAgent();
        PipeletManager manager = new PipeletManager();
    }

    public static void main(String[] args) throws IOException, ExecutionException {
        printLogo();
        run();
    }
}
