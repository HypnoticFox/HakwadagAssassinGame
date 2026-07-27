<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'

import Button from '@/components/Button.vue'
import Input from '@/components/Input.vue'
import { useAuthStore } from '@/stores'

const router = useRouter()
const authStore = useAuthStore()

const email = ref('')
const code = ref('')
const step = ref<'email' | 'code'>('email')
const localError = ref<string | null>(null)

async function onSendOtp() {
  if (!email.value) return
  localError.value = null
  try {
    await authStore.sendOtp(email.value)
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
}
</script>

<template>
  <section class="login-page">
    <div class="login-card">
      <p class="eyebrow">
        Welcome back
      </p>
      <h1 class="login-title">
        Sign in to Hakwadag
      </h1>
      <p class="login-subtitle">
        We'll send a one-time code to your email.
      </p>

      <form
        v-if="step === 'email'"
        class="login-form"
        @submit.prevent="onSendOtp"
      >
        <Input
          v-model="email"
          label="Email"
          type="email"
          placeholder="you@example.com"
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
          Send code
        </Button>
      </form>

      <form
        v-else
        class="login-form"
        @submit.prevent="onVerifyOtp"
      >
        <Input
          v-model="code"
          label="Verification code"
          type="text"
          inputmode="numeric"
          placeholder="123456"
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
          Verify
        </Button>
        <Button
          type="button"
          variant="ghost"
          full-width
          @click="onBack"
        >
          Use a different email
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
