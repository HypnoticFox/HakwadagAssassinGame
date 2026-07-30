<script setup lang="ts">
import { computed } from 'vue'

const props = withDefaults(
  defineProps<{
    modelValue: string
    label?: string
    type?: string
    placeholder?: string
    required?: boolean
    error?: string | null
    autocomplete?: string
    inputmode?: string
    min?: string | number
    max?: string | number
    step?: string | number
  }>(),
  {
    type: 'text',
    label: undefined,
    placeholder: undefined,
    required: false,
    error: null,
    autocomplete: undefined,
    inputmode: undefined,
    min: undefined,
    max: undefined,
    step: undefined,
  },
)

const emit = defineEmits<{
  'update:modelValue': [value: string]
}>()

const model = computed({
  get: () => props.modelValue,
  set: (val: string) => emit('update:modelValue', val),
})
</script>

<template>
  <div class="input-wrapper">
    <label
      v-if="label"
      class="input-label"
    >
      {{ label }}
      <span
        v-if="required"
        class="input-required"
      >*</span>
    </label>
    <input
      v-model="model"
      :type="type"
      :placeholder="placeholder"
      :required="required"
      :autocomplete="autocomplete"
      :inputmode="inputmode as any"
      :min="min"
      :max="max"
      :step="step"
      class="input"
      :class="{ 'input--error': error }"
    >
    <p
      v-if="error"
      class="input-error"
      role="alert"
    >
      {{ error }}
    </p>
  </div>
</template>

<style scoped>
.input-wrapper {
  display: grid;
  gap: 0.375rem;
}

.input-label {
  color: var(--text-secondary);
  font-size: 0.875rem;
  font-weight: 600;
}

.input-required {
  color: var(--danger);
}

.input {
  appearance: none;
  background: var(--surface);
  border: 1px solid var(--border-input);
  border-radius: 0.75rem;
  color: var(--text);
  font-family: inherit;
  font-size: 1rem;
  min-height: 2.875rem;
  padding: 0.75rem 1rem;
  transition:
    border-color 0.15s ease,
    box-shadow 0.15s ease;
  width: 100%;
}

.input:focus {
  border-color: var(--primary);
  box-shadow: 0 0 0 3px var(--primary-ring);
  outline: none;
}

.input--error {
  border-color: var(--danger);
}

.input--error:focus {
  box-shadow: 0 0 0 3px var(--danger-ring);
}

.input-error {
  color: var(--danger);
  font-size: 0.875rem;
  margin: 0;
}

.input::placeholder {
  color: var(--text-faint);
}
</style>
