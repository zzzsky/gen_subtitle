using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Navigation;

namespace GenSubtitle.App.Views;

public partial class TutorialWizardWindow : Window
{
    private int _currentPage = 0;
    private const int TotalPages = 4;

    public TutorialWizardWindow()
    {
        InitializeComponent();
        ShowPage(0);
    }

    private void ShowPage(int pageNumber)
    {
        _currentPage = pageNumber;

        // Update progress dots
        UpdateProgressDots();

        // Show appropriate page
        var page = pageNumber switch
        {
            0 => CreateWelcomePage(),
            1 => CreateImportPage(),
            2 => CreateProcessingPage(),
            3 => CreateCompletionPage(),
            _ => null
        };

        WizardFrame.Content = page;

        // Update button states
        BackButton.IsEnabled = pageNumber > 0;

        if (pageNumber == TotalPages - 1)
        {
            NextButton.Visibility = Visibility.Collapsed;
            FinishButton.Visibility = Visibility.Visible;
        }
        else
        {
            NextButton.Visibility = Visibility.Visible;
            FinishButton.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateProgressDots()
    {
        for (int i = 0; i < TotalPages; i++)
        {
            var dot = FindName($"Dot{i + 1}") as Ellipse;
            if (dot != null)
            {
                dot.Fill = i <= _currentPage
                    ? System.Windows.Media.Brushes.Blue
                    : System.Windows.Media.Brushes.LightGray;
            }
        }
    }

    private Page CreateWelcomePage()
    {
        return new Page
        {
            Content = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "🎬 欢迎使用 GenSubtitle", FontSize = 32, FontWeight = FontWeights.Bold, Margin = new Thickness(0,0,0,24) },
                    new TextBlock { Text = "智能双语字幕生成工具", FontSize = 18, Foreground = System.Windows.Media.Brushes.Gray, Margin = new Thickness(0,0,0,16) },
                    new TextBlock { Text = "本教程将引导您了解主要功能：", FontSize = 14, Margin = new Thickness(0,0,0,16) },
                    new TextBlock { Text = "✓ 导入视频文件", FontSize = 14, Margin = new Thickness(0,0,0,8) },
                    new TextBlock { Text = "✓ 自动转录和翻译", FontSize = 14, Margin = new Thickness(0,0,0,8) },
                    new TextBlock { Text = "✓ 编辑字幕", FontSize = 14, Margin = new Thickness(0,0,0,8) },
                    new TextBlock { Text = "✓ 导出多种格式", FontSize = 14, Margin = new Thickness(0,0,0,8) }
                }
            }
        };
    }

    private Page CreateImportPage()
    {
        return new Page
        {
            Content = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "📁 第一步：导入视频", FontSize = 28, FontWeight = FontWeights.Bold, Margin = new Thickness(0,0,0,24) },
                    new TextBlock { Text = "支持拖拽视频文件到窗口，或点击导入按钮选择文件。", FontSize = 14, Margin = new Thickness(0,0,0,12) },
                    new TextBlock { Text = "支持格式：", FontSize = 14, FontWeight = FontWeights.Bold, Margin = new Thickness(0,16,0,8) },
                    new TextBlock { Text = "MP4, MKV, MOV, AVI, FLV, WMV, WebM, M4V", FontSize = 14, Margin = new Thickness(0,0,0,16) },
                    new TextBlock { Text = "💡 提示：可以一次导入多个视频文件，系统会按顺序处理。", FontSize = 14, FontStyle = FontStyles.Italic, Foreground = System.Windows.Media.Brushes.Gray }
                }
            }
        };
    }

    private Page CreateProcessingPage()
    {
        return new Page
        {
            Content = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "⚙️ 第二步：自动处理", FontSize = 28, FontWeight = FontWeights.Bold, Margin = new Thickness(0,0,0,24) },
                    new TextBlock { Text = "系统会自动完成以下步骤：", FontSize = 14, Margin = new Thickness(0,0,0,16) },
                    new TextBlock { Text = "1. 语音识别 - 使用 Whisper 模型将语音转为文字", FontSize = 14, Margin = new Thickness(0,0,0,8) },
                    new TextBlock { Text = "2. 机器翻译 - 使用 Qwen 模型翻译为中文", FontSize = 14, Margin = new Thickness(0,0,0,8) },
                    new TextBlock { Text = "3. 生成字幕 - 创建双语字幕文件", FontSize = 14, Margin = new Thickness(0,0,0,8) },
                    new TextBlock { Text = "", Margin = new Thickness(0,0,0,16) },
                    new TextBlock { Text = "您可以在处理界面查看进度和状态。", FontSize = 14, Margin = new Thickness(0,0,0,8) },
                    new TextBlock { Text = "处理完成后，点击任务即可进入编辑界面。", FontSize = 14, Margin = new Thickness(0,0,0,8) }
                }
            }
        };
    }

    private Page CreateCompletionPage()
    {
        return new Page
        {
            Content = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "🎉 恭喜！", FontSize = 28, FontWeight = FontWeights.Bold, Margin = new Thickness(0,0,0,24) },
                    new TextBlock { Text = "您已经了解了 GenSubtitle 的基本功能。", FontSize = 16, Margin = new Thickness(0,0,0,16) },
                    new TextBlock { Text = "开始使用吧！", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = System.Windows.Media.Brushes.Blue, Margin = new Thickness(0,0,0,24) },
                    new TextBlock { Text = "更多功能：", FontSize = 14, FontWeight = FontWeights.Bold, Margin = new Thickness(0,0,0,12) },
                    new TextBlock { Text = "• 字幕时间调整", FontSize = 14, Margin = new Thickness(0,0,0,4) },
                    new TextBlock { Text = "• 样式自定义", FontSize = 14, Margin = new Thickness(0,0,0,4) },
                    new TextBlock { Text = "• 批量操作和搜索", FontSize = 14, Margin = new Thickness(0,0,0,4) },
                    new TextBlock { Text = "• 多种导出格式", FontSize = 14, Margin = new Thickness(0,0,0,4) }
                }
            }
        };
    }

    private void OnNext(object sender, RoutedEventArgs e)
    {
        if (_currentPage < TotalPages - 1)
        {
            ShowPage(_currentPage + 1);
        }
    }

    private void OnBack(object sender, RoutedEventArgs e)
    {
        if (_currentPage > 0)
        {
            ShowPage(_currentPage - 1);
        }
    }

    private void OnFinish(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void OnSkip(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
