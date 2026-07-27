<script setup lang="ts">
import { computed } from 'vue'

import { ConditionType, conditionTypeLabel, type ConditionDto } from '@/types'

const props = defineProps<{
  condition: ConditionDto
  selectable?: boolean
  selected?: boolean
}>()

const emit = defineEmits<{
  select: [conditionId: string]
}>()

const displayText = computed(() => {
  if (props.condition.description) {
    return props.condition.description
  }
  switch (props.condition.type) {
    case ConditionType.WithSpecificPerson:
      return `With ${props.condition.targetPersonName || 'a specific person'}`
    case ConditionType.Alone:
      return 'Target is alone'
    case ConditionType.WithXPeople:
      return `With at least ${props.condition.minPeople ?? 2} other people`
    case ConditionType.MundaneAction:
      return `While target is ${props.condition.action || 'doing something'}`
    default:
      return 'Custom condition'
  }
})
</script>

<template>
  <button
    type="button"
    class="condition-card"
    :class="{
      'condition-card--selectable': selectable,
      'condition-card--selected': selected,
    }"
    @click="selectable ? emit('select', condition.id) : null"
  >
    <span class="condition-card__type">{{ conditionTypeLabel(condition.type) }}</span>
    <span class="condition-card__description">
      {{ displayText }}
    </span>
  </button>
</template>

<style scoped>
.condition-card {
  background: white;
  border: 1px solid #e2e8f0;
  border-radius: 0.875rem;
  display: grid;
  gap: 0.375rem;
  padding: 1rem;
  text-align: left;
  transition:
    border-color 0.15s ease,
    box-shadow 0.15s ease;
  width: 100%;
}

.condition-card--selectable {
  cursor: pointer;
}

.condition-card--selectable:hover {
  border-color: #1d4ed8;
}

.condition-card--selected {
  border-color: #1d4ed8;
  box-shadow: 0 0 0 3px rgba(29, 78, 216, 0.15);
}

.condition-card__type {
  color: #1d4ed8;
  font-size: 0.75rem;
  font-weight: 700;
  text-transform: uppercase;
}

.condition-card__description {
  color: #0f172a;
  font-size: 0.9375rem;
  margin: 0;
}
</style>
