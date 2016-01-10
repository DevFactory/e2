package edu.berkeley.span;
import edu.berkeley.span.Request.Command;
import edu.berkeley.span.Request.Command.Builder;

public class BuildTest {
	public static void main(String[] args) {
		Command.Builder builder = Command.newBuilder();
		builder.setCommand(Command.Commands.ListRunning);
		builder.build();
		System.out.println("Hello from java land");
	}
}
