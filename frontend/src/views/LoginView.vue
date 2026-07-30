<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'

import Button from '@/components/Button.vue'
import Input from '@/components/Input.vue'
import { useAuthStore } from '@/stores'

const router = useRouter()
const { t } = useI18n()
const authStore = useAuthStore()

const email = ref(sessionStorage.getItem('login_email') ?? '')
const code = ref('')
const step = ref<'email' | 'code'>(
  (sessionStorage.getItem('login_step') as 'email' | 'code') ?? 'email',
)
const localError = ref<string | null>(null)

watch(step, (v) => sessionStorage.setItem('login_step', v))
watch(email, (v) => sessionStorage.setItem('login_email', v))

onMounted(() => {
  if (step.value === 'code') {
    const sentAt = sessionStorage.getItem('login_otp_sent_at')
    if (!sentAt || !email.value) {
      step.value = 'email'
    } else {
      const elapsed = Date.now() - parseInt(sentAt, 10)
      if (elapsed > 5 * 60 * 1000) {
        step.value = 'email'
        sessionStorage.removeItem('login_step')
        sessionStorage.removeItem('login_email')
        sessionStorage.removeItem('login_otp_sent_at')
        localError.value = t('login.codeExpired')
      }
    }
  }
})

async function onSendOtp() {
  if (!email.value) return
  localError.value = null
  try {
    await authStore.sendOtp(email.value)
    sessionStorage.setItem('login_otp_sent_at', Date.now().toString())
    step.value = 'code'
  } catch (err) {
    if (err instanceof Error) {
      localError.value = err.message
    }
  }
}

async function onVerifyOtp() {
  if (!email.value || !code.value) return
  localError.value = null
  try {
    await authStore.verifyOtp(email.value, code.value)
    sessionStorage.removeItem('login_step')
    sessionStorage.removeItem('login_email')
    sessionStorage.removeItem('login_otp_sent_at')
    await router.push('/')
  } catch (err) {
    if (err instanceof Error) {
      localError.value = err.message
    }
  }
}

function onBack() {
  step.value = 'email'
  code.value = ''
  localError.value = null
  sessionStorage.removeItem('login_step')
  sessionStorage.removeItem('login_email')
  sessionStorage.removeItem('login_otp_sent_at')
}
</script>

<template>
  <section class="login-page">
    <div class="login-card">
      <p class="eyebrow">
        {{ $t('login.eyebrow') }}
      </p>
      <h1 class="login-title">
        {{ $t('login.title') }}
      </h1>
      <p class="login-subtitle">
        {{ $t('login.subtitle') }}
      </p>

      <form
        v-if="step === 'email'"
        class="login-form"
        @submit.prevent="onSendOtp"
      >
        <Input
          v-model="email"
          :label="$t('login.email')"
          type="email"
          :placeholder="$t('login.emailPlaceholder')"
          autocomplete="email"
          required
        />
        <p
          v-if="localError || authStore.error"
          class="form-error"
          role="alert"
        >
          {{ localError || authStore.error }}
        </p>
        <Button
          type="submit"
          size="large"
          full-width
          :loading="authStore.isLoading"
        >
          {{ $t('login.sendCode') }}
        </Button>
      </form>

      <form
        v-else
        class="login-form"
        @submit.prevent="onVerifyOtp"
      >
        <Input
          v-model="code"
          :label="$t('login.verificationCode')"
          type="text"
          inputmode="numeric"
          :placeholder="$t('login.verificationCodePlaceholder')"
          autocomplete="one-time-code"
          required
          :error="null"
        />
        <p
          v-if="localError || authStore.error"
          class="form-error"
          role="alert"
        >
          {{ localError || authStore.error }}
        </p>
        <Button
          type="submit"
          size="large"
          full-width
          :loading="authStore.isLoading"
        >
          {{ $t('login.verify') }}
        </Button>
        <Button
          type="button"
          variant="ghost"
          full-width
          @click="onBack"
        >
          {{ $t('login.useDifferentEmail') }}
        </Button>
      </form>
    </div>
  </section>
</template>

<style scoped>
.login-page {
  align-items: center;
  display: flex;
  justify-content: center;
  min-height: calc(100vh - 4rem);
  padding: 1rem;
}

.login-card {
  background: white;
  border-radius: 1.25rem;
  box-shadow: 0 8px 30px rgba(15, 23, 42, 0.08);
  max-width: 24rem;
  padding: 2rem 1.5rem;
  width: 100%;
}

.login-title {
  font-size: 1.5rem;
  font-weight: 700;
  margin: 0.5rem 0 0;
}

.login-subtitle {
  color: #64748b;
  margin: 0.5rem 0 1.5rem;
}

.login-form {
  display: grid;
  gap: 1rem;
}

.form-error {
  background: #fef2f2;
  border-radius: 0.5rem;
  color: #991b1b;
  font-size: 0.875rem;
  margin: 0;
  padding: 0.75rem;
}

.eyebrow {
  color: #1d4ed8;
  font-size: 0.875rem;
  font-weight: 700;
  margin: 0;
  text-transform: uppercase;
}
</style>
