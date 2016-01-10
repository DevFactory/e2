package edu.berkeley.span;
import edu.berkeley.span.Request.Command;
import edu.berkeley.span.SerializeDeserialize;
import edu.berkeley.span.ServerAgent;
import edu.berkeley.span.ServerAgentException;
import java.util.Arrays;
import java.io.IOException;

public class BuildTest {
	public static void main(String[] args) throws IOException, ServerAgentException {
		ServerAgent s = new ServerAgent();
		System.out.println("Hello from java land");
		System.out.println(s.MachineStatus());
		//s.CreateInstance("test", "test1");
	}
}
