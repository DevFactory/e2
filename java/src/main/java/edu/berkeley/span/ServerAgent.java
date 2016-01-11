package edu.berkeley.span;
import com.google.protobuf.InvalidProtocolBufferException;
import edu.berkeley.span.Request.Command;
import edu.berkeley.span.Resp.*;
import edu.berkeley.span.ServerAgentChannel;
import edu.berkeley.span.SerializeDeserialize;
import edu.berkeley.span.ServerAgentException;
import java.io.IOException;
import java.net.InetSocketAddress;
import java.nio.channels.SocketChannel;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
class ServerAgent {
	private static final int DEFAULT_PORT = 10516;
	private static final String DEFAULT_ADDR = "127.0.0.1";
	private SerializeDeserialize _serde;
	private ServerAgentChannel _channel;
	public ServerAgent(String address, int port) throws IOException {
		_serde = new SerializeDeserialize();
		_channel = new ServerAgentChannel(address, port, _serde);
	}

	public ServerAgent(String address) throws IOException {
		this(address, DEFAULT_PORT);
	}

	public ServerAgent() throws IOException {
		this(DEFAULT_ADDR, DEFAULT_PORT);
	}

	public String MachineStatus() throws IOException, ServerAgentException {
		Command cmd = _serde.MachineStatusString();
		_channel.SendCommand(cmd);
		Response resp = _channel.GetResponse();
		if (resp.getStatus() == Response.Status.StatusString) {
			return resp.getExtension(MachineStatus.ret).getStatus();
		}
		return null;
	}

	public void CreateInstance(String type, String id) throws IOException, ServerAgentException {
		_channel.SendCommand(_serde.NewInstance(type, id));
		_channel.GetResponse();
	}	

	public void StartBess() throws IOException, ServerAgentException {
		_channel.SendCommand(_serde.StartBess());
		_channel.GetResponse();
	}

	public void StopBess() throws IOException, ServerAgentException {
		_channel.SendCommand(_serde.StopBess());
		_channel.GetResponse();
	}

	public void TriggerNotification(NotificationAgent.NotificationType type,
			String pipeletId,
			String nfId) throws IOException, ServerAgentException {
		_channel.SendCommand(_serde.TriggerNotification(type, pipeletId, nfId));
		_channel.GetResponse();
	}
}
