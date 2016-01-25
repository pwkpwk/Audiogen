﻿namespace Audiogen.ViewModels
{
    using Helpers;
    using Audiogen.Models;
    using System;
    using System.Windows.Input;

    sealed class MainPageViewModel : ViewModelBase, IDisposable
    {
        private bool _disposedValue;
        private readonly DelegateCommand _start;
        private readonly DelegateCommand _stop;
        private readonly DelegateCommand _startCompositor;
        private readonly PointerHandler _pointerHandler;
        private IDispatcher _dispatcher;
        private ISynthesizer _synthesizer;
        private SoundSynthesis.Runtime.Compositor _compositor;
        private bool _isInitializing;
        private bool _isReady;
        private bool _isFailed;
        private bool _isRunning;
        private bool _isCompositorRunning;

        public MainPageViewModel()
        {
            _disposedValue = false;
            _start = new DelegateCommand(this.ExecuteStart, this.CanExecuteStart);
            _stop = new DelegateCommand(this.ExecuteStop, this.CanExecuteStop);
            _startCompositor = new DelegateCommand(this.ExecuteStartCompositor, this.CanExecuteStartCompositor);
            _pointerHandler = new PointerHandler();
            _isInitializing = true;
            _isReady = false;
            _isFailed = false;
            _isRunning = false;
            _isCompositorRunning = false;
        }

        ~MainPageViewModel()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IDispatcher Dispatcher
        {
            get { return _dispatcher; }
            set
            {
                if(this.SetProperty(ref _dispatcher, value))
                {
                    SetUpSynthesizerIfFullyComposed();
                }
            }
        }

        public ISynthesizer Synthesizer
        {
            get { return _synthesizer; }
            set
            {
                ISynthesizer oldSynthesizer = _synthesizer;

                if (this.SetProperty(ref _synthesizer, value))
                {
                    if (null != oldSynthesizer)
                    {
                        oldSynthesizer.Ready -= this.OnSynthesizerReady;
                        oldSynthesizer.Failed -= this.OnSynthesizerFailed;
                    }

                    if (null != _synthesizer)
                    {
                        _synthesizer.Ready += this.OnSynthesizerReady;
                        _synthesizer.Failed += this.OnSynthesizerFailed;
                    }

                    SetUpSynthesizerIfFullyComposed();
                }
            }
        }

        public bool IsInitializing
        {
            get { return _isInitializing; }
            private set { this.SetProperty(ref _isInitializing, value); }
        }

        public bool IsReady
        {
            get { return _isReady; }
            private set
            {
                if (this.SetProperty(ref _isReady, value))
                    this.IsInitializing = !(_isReady || _isFailed);
            }
        }

        public bool IsFailed
        {
            get { return _isFailed; }
            private set
            {
                if(this.SetProperty(ref _isFailed, value))
                    this.IsInitializing = !(_isReady || _isFailed);
            }
        }

        public IPointerHandler PointerHandler
        {
            get { return _pointerHandler; }
        }

        public ICommand Start
        {
            get { return _start; }
        }

        public ICommand Stop
        {
            get { return _stop; }
        }

        public ICommand StartCompositor
        {
            get { return _startCompositor; }
        }

        private void SetUpSynthesizerIfFullyComposed()
        {
            if(null != _synthesizer && null != _dispatcher)
            {
                _synthesizer.SetUp();
            }
        }

        private void OnSynthesizerReady(object sender, EventArgs e)
        {
            this.Dispatcher.Dispatch(() =>
            {
                _pointerHandler.SetSynthesizer(_synthesizer);
                this.IsReady = true;
            });
        }

        private void OnSynthesizerFailed(object sender, EventArgs e)
        {
            this.Dispatcher.Dispatch(() => this.IsFailed = true);
        }

        private void ExecuteStart(object parameter)
        {
            _synthesizer.Start();
            _isRunning = true;
            _start.EmitCanExecuteChanged();
            _stop.EmitCanExecuteChanged();
        }

        private bool CanExecuteStart(object parameter)
        {
            return !_isRunning;
        }

        private void ExecuteStop(object parameter)
        {
            _synthesizer.Stop();
            _isRunning = false;
            _start.EmitCanExecuteChanged();
            _stop.EmitCanExecuteChanged();
        }

        private bool CanExecuteStop(object parameter)
        {
            return _isRunning;
        }

        private void ExecuteStartCompositor(object parameter)
        {
            _compositor = SoundSynthesis.Runtime.Compositor.CreateCompositor();
            if(_compositor.Start())
            {
                _isCompositorRunning = true;
                _startCompositor.EmitCanExecuteChanged();
            }
        }

        private bool CanExecuteStartCompositor(object parameter)
        {
            return !_isCompositorRunning;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _disposedValue = true;

                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                _pointerHandler.Dispose();
            }
        }
    }
}
