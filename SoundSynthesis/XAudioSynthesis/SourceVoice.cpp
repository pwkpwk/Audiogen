#include "pch.h"
#include "SourceVoice.h"
#include "AudioEngine.h"

using namespace SoundSynthesis::XAudioSynthesis;

SourceVoice::SourceVoice(_In_ AudioEngine *engine, _In_ AudioFrameSource *frameSource) noexcept
:	m_started(0),
	m_engine(engine),
	m_frameSource(frameSource),
	m_voice(nullptr),
	m_eofSent(false),
	m_eofBytes(0)
{
}

SourceVoice::~SourceVoice() noexcept
{
	_ASSERTE(nullptr == m_voice);
}

_Check_return_
bool SourceVoice::CreateResources(_In_ IXAudio2 *xaudio, _In_ IXAudio2Voice *output, const WAVEFORMATEX *waveFormat) noexcept
{
	XAUDIO2_SEND_DESCRIPTOR	sd[1] = { 0 };
	XAUDIO2_VOICE_SENDS		sends[1] = { 0 };

	sends->SendCount = _ARRAYSIZE(sd);
	sends->pSends = sd;
	sd->pOutputVoice = output;
	m_eofBytes = waveFormat->nBlockAlign;

	return SUCCEEDED(xaudio->CreateSourceVoice(&m_voice, waveFormat, 0, XAUDIO2_DEFAULT_FREQ_RATIO, this, sends, NULL));
}

void SourceVoice::TearDown() noexcept
{
	_ASSERTE(0 == m_started);
	_ASSERTE(nullptr != m_voice);
	m_voice->DestroyVoice();
	m_voice = nullptr;
}

_Check_return_
bool SourceVoice::Start() noexcept
{
	_ASSERTE(nullptr != m_voice);

	bool started = false;

	if (0 == _InterlockedCompareExchange16(&m_started, 1, 0))
	{
		m_voice->FlushSourceBuffers();
		m_eofSent = false;

		if (SUCCEEDED(m_voice->Start()))
		{
			started = true;
		}

		if (!started)
		{
			_InterlockedExchange16(&m_started, 0);
		}
	}

	return started;
}

void SourceVoice::Stop() noexcept
{
	if (1 == _InterlockedCompareExchange16(&m_started, 0, 1))
	{
		_ASSERTE(nullptr != m_voice);
	}
}

void SourceVoice::OnVoiceProcessingPassStart(UINT32 bytesRequired) noexcept
{
	UNUSED(bytesRequired);

	if (0 == m_started)
	{
		if (!m_eofSent)
		{
			XAUDIO2_BUFFER *buffer = m_frameSource->Allocate(XAUDIO2_END_OF_STREAM, m_eofBytes);
			m_eofSent = m_frameSource->Submit(buffer, m_voice);
		}
	}
	else
	{
		size_t samples;
		XAUDIO2_BUFFER *frame = m_frameSource->Allocate(0, bytesRequired);
		SampleWriter writer = m_frameSource->GetSampleWriter(frame, &samples);

		for (size_t s = 0; s < samples; ++s)
		{
			writer.Write((static_cast<float>(rand() % 100) - 50.0f) / 100.0f, 1);
		}

		m_frameSource->Submit(frame, m_voice);
	}
}

void SourceVoice::OnVoiceProcessingPassEnd() noexcept
{
	if (m_eofSent)
	{
		m_voice->Stop();
		m_engine->RecycleSourceVoice(this);
	}
}

void SourceVoice::OnStreamEnd() noexcept
{
}

void SourceVoice::OnBufferStart(void *pBufferContext) noexcept
{
	UNUSED(pBufferContext);
}

void SourceVoice::OnBufferEnd(void *pBufferContext) noexcept
{
	m_frameSource->Release(AudioFrameSource::FromContext(pBufferContext));
}

void SourceVoice::OnLoopEnd(void *pBufferContext) noexcept
{
	UNUSED(pBufferContext);
}

void SourceVoice::OnVoiceError(void *pBufferContext, HRESULT error) noexcept
{
	UNUSED(pBufferContext);
	UNUSED(error);
}
