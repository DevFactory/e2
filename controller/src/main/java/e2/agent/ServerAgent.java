package e2.agent;

import java.io.IOException;

import e2.pipelet.PipeletType;
import e2.proto.agent.Request;
import e2.proto.agent.Resp;

public class ServerAgent {
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
        Request.Command cmd = _serde.MachineStatusString();
        _channel.SendCommand(cmd);
        Resp.Response resp = _channel.GetResponse();
        if (resp.getStatus() == Resp.Response.Status.StatusString) {
            return resp.getExtension(Resp.MachineStatus.ret).getStatus();
        }
        return null;
    }

    public void CreateInstance(String type, String id) throws IOException, ServerAgentException {
        _channel.SendCommand(_serde.NewInstance(type, id));
        _channel.GetResponse();
    }

    public void DestroyInstance(String id) throws IOException, ServerAgentException {
        _channel.SendCommand(_serde.KillInstance(id));
        _channel.GetResponse();
    }

    public void NewPipelet(String type, PipeletType pipelet)
            throws IOException, ServerAgentException {
        _channel.SendCommand(_serde.NewPipelet(type, pipelet.getNodes(), pipelet.getEdges(), pipelet.getExternalFilter()));
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
