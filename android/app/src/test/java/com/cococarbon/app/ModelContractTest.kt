package com.cococarbon.app

import com.cococarbon.app.data.CarbonEstimateRequest
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Test

class ModelContractTest {
    @Test
    fun calculatorRequest_disablesPerTreeAllocationByDefault() {
        val req = CarbonEstimateRequest(
            farmId = null,
            areaHa = 1.0,
            treeCount = 200,
            variety = "TallDuaTa",
            treeAgeYears = 10
        )
        assertFalse(req.includeArithmeticPerTreeAllocation)
        assertEquals("TallDuaTa", req.variety)
        assertEquals(10, req.treeAgeYears)
    }
}
