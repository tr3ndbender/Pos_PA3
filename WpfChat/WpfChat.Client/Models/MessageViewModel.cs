using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfChat.Client.Helpers;
using WpfChat.Shared.Models;

namespace WpfChat.Client.Models
{
    public class MessageViewModel : INotifyPropertyChanged
    {
        private readonly ChatMessageDto _dto;

        public MessageViewModel(ChatMessageDto dto)
        {
            _dto = dto;
            UpdateAvatar();
        }

        public string SenderUsername => _dto.SenderUsername;
        public string Content => _dto.Content;
        public DateTime Timestamp => _dto.Timestamp;
        public string? RecipientUsername => _dto.RecipientUsername;
        public bool IsPrivate => _dto.IsPrivate;
        public string SenderColor => _dto.SenderColor;

        public SolidColorBrush SenderColorBrush
        {
            get
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(_dto.SenderColor);
                    return new SolidColorBrush(color);
                }
                catch { return Brushes.Black; }
            }
        }

        private BitmapImage? _avatarImage;
        public BitmapImage? SenderAvatarImage
        {
            get => _avatarImage;
            private set { _avatarImage = value; OnPropertyChanged(); }
        }

        public void UpdateAvatar()
        {
            if (!string.IsNullOrEmpty(_dto.SenderProfileImageBase64))
                SenderAvatarImage = ImageHelper.FromBase64(_dto.SenderProfileImageBase64, 50);
        }

        public void SetProfileImage(string base64)
        {
            _dto.SenderProfileImageBase64 = base64;
            UpdateAvatar();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class OnlineUserViewModel : INotifyPropertyChanged
    {
        private UserDto _user;

        public OnlineUserViewModel(UserDto user)
        {
            _user = user;
            UpdateAvatar();
        }

        public string Username => _user.Username;

        public SolidColorBrush ColorBrush
        {
            get
            {
                try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(_user.Color)); }
                catch { return Brushes.Black; }
            }
        }

        private BitmapImage? _avatar;
        public BitmapImage? Avatar
        {
            get => _avatar;
            private set { _avatar = value; OnPropertyChanged(); }
        }

        public void UpdateFrom(UserDto user)
        {
            _user = user;
            OnPropertyChanged(nameof(ColorBrush));
            UpdateAvatar();
        }

        private void UpdateAvatar()
        {
            if (!string.IsNullOrEmpty(_user.ProfileImageBase64))
                Avatar = ImageHelper.FromBase64(_user.ProfileImageBase64, 30);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
