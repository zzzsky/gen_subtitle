using GenSubtitle.Core.Models;

namespace GenSubtitle.App.ViewModels;

public sealed class SubtitleSegmentViewModel : ObservableObject
{
    private readonly SubtitleSegment _segment;

    public SubtitleSegmentViewModel(SubtitleSegment segment)
    {
        _segment = segment;
    }

    public int Id
    {
        get => _segment.Id;
        set
        {
            if (_segment.Id != value)
            {
                _segment.Id = value;
                RaisePropertyChanged();
            }
        }
    }

    public TimeSpan Start
    {
        get => _segment.Start;
        set
        {
            if (_segment.Start != value)
            {
                _segment.Start = value;
                RaisePropertyChanged();
            }
        }
    }

    public TimeSpan End
    {
        get => _segment.End;
        set
        {
            if (_segment.End != value)
            {
                _segment.End = value;
                RaisePropertyChanged();
            }
        }
    }

    public string SourceText
    {
        get => _segment.SourceText;
        set
        {
            if (_segment.SourceText != value)
            {
                _segment.SourceText = value;
                RaisePropertyChanged();
            }
        }
    }

    public string ZhText
    {
        get => _segment.ZhText;
        set
        {
            if (_segment.ZhText != value)
            {
                _segment.ZhText = value;
                RaisePropertyChanged();
            }
        }
    }

    public float Confidence
    {
        get => _segment.Confidence;
        set
        {
            if (Math.Abs(_segment.Confidence - value) > 0.001f)
            {
                _segment.Confidence = value;
                RaisePropertyChanged();
            }
        }
    }

    public SubtitleSegment ToModel() => _segment;
}
