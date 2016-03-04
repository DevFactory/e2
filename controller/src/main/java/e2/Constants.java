package e2;

public final class Constants {
    public static final int KB = 1024;
    public static final int MB = KB * 1024;
    public static final int GB = MB * 1024;

    public static final String WEB_PORT = "e2.web.port";
    public static final String WEB_THREAD_COUNT = "e2.web.thread.count";

    public static final String SWITCH_HOSTNAME = "e2.switch.hostname";

    public static final String SERVER_COUNT = "e2.server.count";

    public static final String LOGO =
            "      __         __  _              __       \n" +
                    " ___ / /__ ____ / /_(_)___  ___ ___/ /__ ____ \n" +
                    "/ -_) / _ `(_-</ __/ / __/ / -_) _  / _ `/ -_)\n" +
                    "\\__/_/\\_,_/___/\\__/_/\\__/  \\__/\\_,_/\\_, /\\__/ \n" +
                    "                                   /___/    \n";


    public static final String EXAMPLE_POLICY =
            "isonf fw\n" +
                    "isonf ids\n" +
                    "isonf nat\n" +
                    "pipeline default {\n" +
                    "  inf: fw[0]\n" +
                    "  inr: nat[1]\n" +
                    "  out: fw[0] nat[1]\n" +
                    "  fw[1][\"dst port 80\"] -> ids[0]\n" +
                    "  fw[1][\"!(dst port 80)\"] -> nat[0]\n" +
                    "  ids[1] -> nat[0]\n" +
                    "  nat[0][\"src port 80\"] -> ids[1]\n" +
                    "  nat[0][\"!(src port 80)\"] -> fw[1]\n" +
                    "  ids[0] -> fw[1]\n" +
                    "}\n";
}
