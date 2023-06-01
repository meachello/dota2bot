namespace Dota_API;

public class AnalyzingParametrs
{
    private int _duration;
    private int _sentries;
    private int _observers;

    public AnalyzingParametrs()
    {
        
    }
    public AnalyzingParametrs(int duration)
    {
        _duration = duration;
    }

    public int AvgSentryPerMin()
    {
        _sentries = (_duration / 60)/2;
        return _sentries;
    }

    public int AvgObsPerMin()
    {
        _observers = (_duration / 60)/3;
        return _observers;
    }
    
}