using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WpfChat.Client.Models
{
    public class ChatTabItem : INotifyPropertyChanged
    {
        public string Name { get; set; } = "";
        public bool IsPrivate { get; set; }
        public ObservableCollection<MessageViewModel> Messages { get; } = new();

        private bool _hasUnread;
        public bool HasUnread
        {
            get => _hasUnread;
            set { _hasUnread = value; OnPropertyChanged(); OnPropertyChanged(nameof(HeaderText)); }
        }

        public string HeaderText => HasUnread ? $"● {Name}" : Name;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
